#!/usr/bin/env node

/**
 * QX Workflow Stop Hook — AI 回答完后触发记录 + 推荐下一步（Stop）
 *
 * 职责:
 * 1. 当 AI 完成一轮回答后，注入记录指令
 * 2. 根据工作流转换表推荐下一步命令
 * 3. 注入当前活跃变更上下文
 *
 * 配对: UserPromptSubmit(标记) + Stop(触发记录)
 * 一次性触发: block 后立即清除 pending_record，避免无限循环
 *
 * 状态文件:
 *   .claude/qx/state/workflow-record.json (读写)
 *   .claude/qx/state/loop.json (只读)
 */

import { readFileSync, writeFileSync, existsSync } from "fs";
import { resolve } from "path";

// ─── 工作流转换表 ───
// key = "command" 或 "command subcommand"，value = 推荐的下一步
const WORKFLOW_TRANSITIONS = {
  // 策划阶段
  "/qx-gd-brainstorm": "/qx-gd-gdd 或 /qx-gd-narrative",
  "/qx-gd-gdd": "/qx-meeting（三方评审）",
  "/qx-gd-narrative": "/qx-meeting（三方评审）",
  "/qx-meeting": "/qx-gd-stories（拆分 Story）或 /qx-dev-change create",
  "/qx-gd-stories": "/qx-gd-sprint plan（Sprint 规划）",
  "/qx-gd-sprint": "/qx-dev-change create（开始第一个变更）",

  // 变更生命周期
  "/qx-dev-change create": "/qx-dev-change propose",
  "/qx-dev-change propose": "/qx-dev-change design 或 /qx-meeting",
  "/qx-dev-change spec": "/qx-dev-change design",
  "/qx-dev-change design": "/qx-dev-plan（编写实施计划）",
  "/qx-dev-change verify": "/qx-finishing（收尾）",
  "/qx-dev-change archive": "/qx-compound（复合积累）",

  // 实施阶段
  "/qx-dev-plan": "/qx-dev-exec（执行计划）",
  "/qx-dev-exec": "/qx-verify（完成前验证）",
  "/qx-verify": "/qx-finishing（收尾）",
  "/qx-finishing": "/qx-dev-change archive（归档变更）",
  "/qx-compound": "下一个 Story → /qx-dev-change create",

  // 其他
  "/qx-dev-impact": "根据影响分析结果决定下一步",
  "/qx-dev-debug": "修复后 → /qx-verify（验证修复）",
  "/qx-qa-test design": "/qx-qa-test automate（自动化测试）",
  "/qx-qa-test automate": "/qx-qa-test execute（执行测试）",
  "/qx-qa-test execute": "/qx-verify（验证结果）",
  "/qx-art-ux": "/qx-meeting（评审 UX 方案）",
};

// 这些子命令完成后需要提醒用户 git commit 变更文档
const COMMIT_AFTER_SUBCOMMANDS = new Set(["create", "propose", "spec", "design"]);

// ─── 主逻辑 ───

const input = JSON.parse(await readStdin());
const cwd = input.cwd || process.cwd();
const sessionId = input.session_id || "";

const recordFile = resolve(cwd, ".claude/qx/state/workflow-record.json");
const loopFile = resolve(cwd, ".claude/qx/state/loop.json");

// 加载 workflow-record 状态
let recordState = null;
if (existsSync(recordFile)) {
  try {
    recordState = JSON.parse(readFileSync(recordFile, "utf8"));
  } catch {
    recordState = null;
  }
}

// 无状态或无待记录 → 放行
if (!recordState || !recordState.pending_record) {
  allow();
}

// Session 隔离
if (recordState.session_id && recordState.session_id !== sessionId) {
  allow();
}

// 检查 loop 是否活跃
let loopActive = false;
if (existsSync(loopFile)) {
  try {
    const loopState = JSON.parse(readFileSync(loopFile, "utf8"));
    loopActive = loopState.active === true;
  } catch {
    loopActive = false;
  }
}

// Loop 活跃 → 让 loop hook 接管
if (loopActive) {
  allow();
}

// ═══ 去重：同一 trigger 短时间内不重复 block ═══
const DEDUP_WINDOW_MS = 5 * 60 * 1000; // 5 分钟
const currentTrigger = recordState.last_trigger || "";
const lastBlocked = recordState._last_blocked_trigger || "";
const lastBlockedAt = recordState._last_blocked_at || "";

if (
  currentTrigger === lastBlocked &&
  lastBlockedAt &&
  Date.now() - new Date(lastBlockedAt).getTime() < DEDUP_WINDOW_MS
) {
  // 同一 trigger 已在窗口内 block 过 → 清除 pending 并放行
  recordState.pending_record = false;
  try {
    writeFileSync(recordFile, JSON.stringify(recordState, null, 2));
  } catch {
    /* best effort */
  }
  allow();
}

// ═══ 触发记录：清除标志 + 记录去重信息 + 注入记录指令 ═══
recordState.pending_record = false;
recordState._last_blocked_trigger = currentTrigger;
recordState._last_blocked_at = new Date().toISOString();
try {
  writeFileSync(recordFile, JSON.stringify(recordState, null, 2));
} catch {
  /* best effort */
}

const logFile =
  recordState.log_file || ".claude/memory/qx-a1-workflow-analysis.md";
const trigger = recordState.last_trigger || "(unknown)";
const preview = recordState.trigger_preview || "";
const stepNum = recordState.step_count || "?";
const planFile = recordState.plan_file || "";
const activeChanges = recordState.active_changes || [];
const lastCommand = recordState.last_command || "";
const lastSubcommand = recordState.last_subcommand || "";

// 查找推荐下一步
const transitionKey = lastSubcommand
  ? `${lastCommand} ${lastSubcommand}`
  : lastCommand;
const recommendedNext =
  WORKFLOW_TRANSITIONS[transitionKey] ||
  WORKFLOW_TRANSITIONS[lastCommand] ||
  "";

// 构建上下文块
const planContext = planFile ? `**执行计划**: ${planFile}\n` : "";

const changesContext =
  activeChanges.length > 0
    ? `**当前活跃变更**: ${activeChanges.join(", ")}\n`
    : "";

// 构建推荐下一步
const nextLine = recommendedNext
  ? `\u{1F449} 推荐下一步: ${recommendedNext}\n`
  : "";

// 构建 git commit 提醒
const commitReminder =
  lastCommand === "/qx-dev-change" && COMMIT_AFTER_SUBCOMMANDS.has(lastSubcommand)
    ? `\n\u{1F4A1} 变更文档已生成，请 git commit 相关文件（.change.yaml / proposal.md / design.md 等）\n`
    : "";

// 构建轻量记录指令
const recordLine =
  `\n\u{1F4DD} 请追加一行记录到 ${logFile}（表格格式）：\n` +
  `| ${stepNum} | ${trigger} | (一句话摘要) | (遇到的问题，如无则留空) |\n`;

process.stdout.write(
  JSON.stringify({
    decision: "block",
    reason:
      `<qx-workflow-record>\n` +
      `\u2705 ${trigger} 已完成\n` +
      nextLine +
      commitReminder +
      planContext +
      changesContext +
      recordLine +
      `</qx-workflow-record>`,
  })
);
process.exit(0);

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
