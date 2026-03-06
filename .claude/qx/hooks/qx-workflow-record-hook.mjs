#!/usr/bin/env node

/**
 * QX Workflow Record Hook — 工作流步骤标记（UserPromptSubmit）
 *
 * 职责:
 * 1. 在用户输入时标记"本轮需要记录"（pending_record）
 * 2. 捕获 /qx-dev-create 子命令（create/propose/design/verify/archive）
 * 3. 扫描 docs/changes/ 识别当前活跃变更
 * 4. 从 /qx-dev-exec 参数提取 plan 文件路径
 *
 * 实际的记录+推荐下一步由 qx-workflow-stop-hook.mjs 在 AI 回答完后触发。
 *
 * 配对: UserPromptSubmit(标记) + Stop(触发记录)
 * 状态文件: .claude/qx/state/workflow-record.json
 * 超时: 2 小时无 QX 命令后自动停用
 */

import {
  readFileSync,
  writeFileSync,
  existsSync,
  mkdirSync,
  readdirSync,
  statSync,
} from "fs";
import { resolve } from "path";

const input = JSON.parse(await readStdin());
const cwd = input.cwd || process.cwd();
const sessionId = input.session_id || "";
const userPrompt = input.user_prompt || input.prompt || "";

const stateDir = resolve(cwd, ".claude/qx/state");
const stateFile = resolve(stateDir, "workflow-record.json");

// ─── 命令解析 ───

// 检测 QX 命令 — 只匹配输入开头，忽略对话中的提及
const qxMatch = userPrompt.match(/^\s*\/(qx-[\w-]+)/);
const isQxCommand = !!qxMatch;
const commandName = qxMatch ? `/${qxMatch[1]}` : null;

// 捕获 /qx-dev-create 子命令 (spec)
const changeSubMatch = userPrompt.match(
  /^\s*\/qx-dev-create\s+(spec)/i
);
const subcommand = changeSubMatch ? changeSubMatch[1].toLowerCase() : null;

// 从 /qx-dev-exec <plan-path> 中提取 plan 文件路径
const planMatch = userPrompt.match(
  /^\s*\/qx-dev-exec\s+(?:--\S+\s+)*([\w.\/\\-]+plan[\w.\/\\-]*)/i
);
const planFile = planMatch ? planMatch[1] : null;

// ─── 扫描活跃变更 ───

function scanActiveChanges() {
  const changesDir = resolve(cwd, "docs/changes");
  if (!existsSync(changesDir)) return [];
  try {
    return readdirSync(changesDir).filter((entry) => {
      if (entry === "archive" || entry.startsWith(".")) return false;
      const full = resolve(changesDir, entry);
      try {
        return statSync(full).isDirectory();
      } catch {
        return false;
      }
    });
  } catch {
    return [];
  }
}

// ─── 加载状态 ───

let state = {
  active: false,
  session_id: "",
  pending_record: false,
  last_command: "",
  last_subcommand: "",
  last_trigger: "",
  trigger_timestamp: "",
  trigger_preview: "",
  last_timestamp: "",
  log_file: "",
  plan_file: "",
  active_changes: [],
  step_count: 0,
  steps: [],
};

if (existsSync(stateFile)) {
  try {
    state = JSON.parse(readFileSync(stateFile, "utf8"));
  } catch {
    /* corrupt → use defaults */
  }
}

// 判断工作流是否活跃（2 小时超时）
const TWO_HOURS = 2 * 60 * 60 * 1000;
const isActive =
  state.active &&
  state.last_timestamp &&
  Date.now() - new Date(state.last_timestamp).getTime() < TWO_HOURS;

if (isQxCommand) {
  // ═══ QX 命令 → 激活 + 标记 + 扫描活跃变更 ═══
  ensureDir(stateDir);

  const activeChanges = scanActiveChanges();

  state.active = true;
  state.session_id = sessionId;
  state.pending_record = true;
  state.last_command = commandName;
  state.last_subcommand = subcommand || "";
  state.last_trigger = subcommand
    ? `${commandName} ${subcommand}`
    : commandName;
  state.trigger_timestamp = new Date().toISOString();
  state.trigger_preview = userPrompt.substring(0, 200);
  state.last_timestamp = new Date().toISOString();
  state.step_count = (state.step_count || 0) + 1;
  state.active_changes = activeChanges;
  if (planFile) state.plan_file = planFile;

  state.steps = state.steps || [];
  state.steps.push({
    n: state.step_count,
    cmd: state.last_trigger,
    ts: new Date().toISOString(),
    preview: userPrompt.substring(0, 120),
  });
  if (state.steps.length > 100) state.steps = state.steps.slice(-100);

  safeWrite(stateFile, state);
  allow();
} else if (isActive) {
  // ═══ 活跃工作流的后续输入 → 有条件标记 ═══

  // 去重：跳过不应触发记录的后续消息
  const isTaskNotification = userPrompt.includes("<task-notification>");
  const isHookFeedback = userPrompt.includes("<qx-workflow-record>");
  const skipRecord = isTaskNotification || isHookFeedback;

  if (!skipRecord) {
    state.pending_record = true;
  }
  // 即使跳过记录，也更新时间戳保持活跃状态
  state.last_trigger = `(follow-up: ${state.last_command}${state.last_subcommand ? " " + state.last_subcommand : ""})`;
  state.trigger_timestamp = new Date().toISOString();
  state.trigger_preview = userPrompt.substring(0, 200);
  state.last_timestamp = new Date().toISOString();
  state.step_count = (state.step_count || 0) + 1;

  state.steps = state.steps || [];
  state.steps.push({
    n: state.step_count,
    cmd: skipRecord ? "(skip-notification)" : "(follow-up)",
    ts: new Date().toISOString(),
    preview: userPrompt.substring(0, 120),
  });
  if (state.steps.length > 100) state.steps = state.steps.slice(-100);

  safeWrite(stateFile, state);
  allow();
} else {
  // ═══ 无活跃工作流 → 超时自动停用 ═══
  if (state.active && !isActive) {
    state.active = false;
    safeWrite(stateFile, state);
  }
  allow();
}

// ─── 工具函数 ───

function allow() {
  process.stdout.write(JSON.stringify({ decision: "allow" }));
  process.exit(0);
}

function ensureDir(dir) {
  try {
    if (!existsSync(dir)) mkdirSync(dir, { recursive: true });
  } catch {
    /* best effort */
  }
}

function safeWrite(path, data) {
  try {
    writeFileSync(path, JSON.stringify(data, null, 2));
  } catch {
    /* best effort */
  }
}

async function readStdin() {
  const chunks = [];
  for await (const chunk of process.stdin) chunks.push(chunk);
  return Buffer.concat(chunks).toString("utf8");
}
