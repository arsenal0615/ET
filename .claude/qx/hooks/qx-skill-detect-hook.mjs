#!/usr/bin/env node

/**
 * QX Skill Detect Hook — 检测 AI 内部调用 QX Skill（PreToolUse: Skill）
 *
 * 场景: 用户输入"下一步"→ AI 调用 Skill(qx-dev-change, args="design")
 *        → 本 hook 将 workflow-record 状态更新为 /qx-dev-change design
 *        → stop hook 就能正确推荐下一步 + 触发 git commit 提醒
 *
 * 不影响用户直接输入 /qx-* 的场景（UserPromptSubmit hook 已处理，
 * 本 hook 会用相同数据覆盖，无冲突）。
 *
 * 配对: UserPromptSubmit(标记) + PreToolUse/Skill(检测) + Stop(触发记录)
 * 状态文件: .claude/qx/state/workflow-record.json
 */

import { readFileSync, writeFileSync, existsSync, mkdirSync } from "fs";
import { resolve } from "path";

const input = JSON.parse(await readStdin());
const toolName = input.tool_name || "";
const toolInput = input.tool_input || {};
const cwd = input.cwd || process.cwd();
const sessionId = input.session_id || "";

// 只处理 Skill 工具
if (toolName !== "Skill") {
  allow();
}

const skillName = toolInput.skill || "";
const skillArgs = toolInput.args || "";

// 只处理 qx-* skills
if (!skillName.startsWith("qx-")) {
  allow();
}

// ─── 解析命令名和子命令 ───

const commandName = `/${skillName}`;

// 对 qx-dev-change，从 args 提取子命令
let subcommand = "";
if (skillName === "qx-dev-change") {
  const subMatch = skillArgs.match(
    /^\s*(create|propose|spec|design|verify|archive|status|list)/i
  );
  subcommand = subMatch ? subMatch[1].toLowerCase() : "";
}

// 对 qx-qa-test，从 args 提取子命令
if (skillName === "qx-qa-test") {
  const subMatch = skillArgs.match(/^\s*(design|automate|execute)/i);
  subcommand = subMatch ? subMatch[1].toLowerCase() : "";
}

// 对 qx-gd-sprint，从 args 提取子命令
if (skillName === "qx-gd-sprint") {
  const subMatch = skillArgs.match(/^\s*(plan|status|retro)/i);
  subcommand = subMatch ? subMatch[1].toLowerCase() : "";
}

// ─── 更新状态 ───

const stateDir = resolve(cwd, ".claude/qx/state");
const stateFile = resolve(stateDir, "workflow-record.json");

let state = {};
if (existsSync(stateFile)) {
  try {
    state = JSON.parse(readFileSync(stateFile, "utf8"));
  } catch {
    /* corrupt → use defaults */
  }
}

try {
  if (!existsSync(stateDir)) mkdirSync(stateDir, { recursive: true });
} catch {
  /* best effort */
}

// 如果 UserPromptSubmit 未激活工作流（用户输入了"下一步"等非 QX 文本），
// 在这里激活并递增 step_count
if (!state.active) {
  state.active = true;
  state.step_count = (state.step_count || 0) + 1;
}

// 覆盖命令元数据 — 确保 stop hook 拿到的是实际执行的 skill，
// 而非 UserPromptSubmit 记录的 follow-up
state.session_id = sessionId;
state.pending_record = true;
state.last_command = commandName;
state.last_subcommand = subcommand;
state.last_trigger = subcommand
  ? `${commandName} ${subcommand}`
  : commandName;
state.last_timestamp = new Date().toISOString();

// 不覆盖 trigger_preview — 保留用户原始输入（如"下一步"）

try {
  writeFileSync(stateFile, JSON.stringify(state, null, 2));
} catch {
  /* best effort */
}

allow();

// ─── 工具函数 ───

function allow() {
  process.stdout.write(JSON.stringify({ decision: "allow" }));
  process.exit(0);
}

async function readStdin() {
  const chunks = [];
  for await (const chunk of process.stdin) chunks.push(chunk);
  return Buffer.concat(chunks).toString("utf8");
}
