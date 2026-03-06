#!/usr/bin/env node

/**
 * QX Loop Hook — 持久化执行循环
 *
 * Stop hook: 当 Agent 轮次结束时触发。
 * 如果 loop 状态为 active，注入继续消息让 Agent 继续工作。
 *
 * 状态文件: .claude/qx/state/loop.json
 * 安全机制: session_id 隔离、2小时超时、max_iterations 限制
 */

import { readFileSync, writeFileSync, existsSync } from "fs";
import { resolve } from "path";

// 从 stdin 读取 hook 输入
const input = JSON.parse(await readStdin());

const cwd = input.cwd || process.cwd();
const stateFile = resolve(cwd, ".claude/qx/state/loop.json");

// 无状态文件 → 正常停止
if (!existsSync(stateFile)) {
  process.stdout.write(JSON.stringify({ decision: "allow" }));
  process.exit(0);
}

let state;
try {
  state = JSON.parse(readFileSync(stateFile, "utf8"));
} catch {
  process.stdout.write(JSON.stringify({ decision: "allow" }));
  process.exit(0);
}

// 非活跃 → 正常停止
if (!state.active) {
  process.stdout.write(JSON.stringify({ decision: "allow" }));
  process.exit(0);
}

// Session 隔离: 不同会话不互相干扰
const sessionId = input.session_id || "";
if (state.session_id && state.session_id !== sessionId) {
  process.stdout.write(JSON.stringify({ decision: "allow" }));
  process.exit(0);
}

// 超时保护: 2 小时
const startedAt = new Date(state.started_at).getTime();
const maxDuration = 2 * 60 * 60 * 1000; // 2 hours
if (Date.now() - startedAt > maxDuration) {
  state.active = false;
  state.stopped_reason = "timeout";
  writeFileSync(stateFile, JSON.stringify(state, null, 2));
  process.stdout.write(JSON.stringify({ decision: "allow" }));
  process.exit(0);
}

// 迭代限制
const maxIterations = state.max_iterations || 50;
if ((state.iteration || 0) >= maxIterations) {
  state.active = false;
  state.stopped_reason = "max_iterations";
  writeFileSync(stateFile, JSON.stringify(state, null, 2));
  process.stdout.write(JSON.stringify({ decision: "allow" }));
  process.exit(0);
}

// 继续执行: 递增迭代，注入继续消息
state.iteration = (state.iteration || 0) + 1;
state.last_continued_at = new Date().toISOString();
writeFileSync(stateFile, JSON.stringify(state, null, 2));

const planPath = state.plan_path || "the current plan";
const message = `QX Loop: 继续执行 ${planPath}（迭代 ${state.iteration}/${maxIterations}）。检查进度并继续下一个任务。如所有任务完成，运行 /qx-dev-stop。`;

process.stdout.write(
  JSON.stringify({
    decision: "block",
    reason: message,
  })
);
process.exit(0);

// --- 工具函数 ---

async function readStdin() {
  const chunks = [];
  for await (const chunk of process.stdin) {
    chunks.push(chunk);
  }
  return Buffer.concat(chunks).toString("utf8");
}
