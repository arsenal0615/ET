#!/usr/bin/env node

/**
 * QX Guard Hook — ET 规则守卫（PreToolUse）
 *
 * 在 Write/Edit 工具执行前检查文件路径是否违反四程序集分离规则。
 * 仅检查 Scripts/ 目录下的 .cs 文件。
 *
 * 规则:
 * - Model/ 目录下不应有 System 类（文件名含 System 且不含 Component）
 * - Hotfix/ 目录下不应有 Component 定义（文件名含 Component 且不含 System）
 * - Client/ 目录下的文件不应在 Server 命名空间
 *
 * 这是一个轻量级启发式检查，不替代编译期的 Source Generator 验证。
 */

import { readFileSync } from "fs";

// 从 stdin 读取 hook 输入
const input = JSON.parse(await readStdin());

const toolName = input.tool_name || "";
const toolInput = input.tool_input || {};

// 只检查 Write 和 Edit 工具
if (toolName !== "Write" && toolName !== "Edit") {
  process.stdout.write(JSON.stringify({ decision: "allow" }));
  process.exit(0);
}

const filePath = toolInput.file_path || "";

// 只检查 .cs 文件在 Scripts/ 目录下
if (!filePath.endsWith(".cs") || !filePath.includes("Scripts/")) {
  process.stdout.write(JSON.stringify({ decision: "allow" }));
  process.exit(0);
}

const warnings = [];

// 检查 Model 目录中是否有 System 文件（不含 Component）
if (filePath.includes("/Model/") || filePath.includes("/ModelView/")) {
  const fileName = filePath.split("/").pop();
  if (
    fileName.includes("System") &&
    !fileName.includes("Component") &&
    !fileName.includes("EventSystem")
  ) {
    warnings.push(
      `⚠️ 文件 ${fileName} 看起来是 System 类，但位于 Model 程序集。System 逻辑应放在 Hotfix 程序集。`
    );
  }
}

// 检查 Hotfix 目录中是否有 Component 定义文件
if (filePath.includes("/Hotfix/") || filePath.includes("/HotfixView/")) {
  const fileName = filePath.split("/").pop();
  if (fileName.includes("Component") && !fileName.includes("System")) {
    warnings.push(
      `⚠️ 文件 ${fileName} 看起来是 Component 定义，但位于 Hotfix 程序集。Component 数据定义应放在 Model 程序集。`
    );
  }
}

// 检查内容中的命名空间（如果是 Write 且有 content）
if (toolName === "Write" && toolInput.content) {
  const content = toolInput.content;

  // Client 目录中不应有 ET.Server 命名空间
  if (filePath.includes("/Client/") && content.includes("namespace ET.Server")) {
    warnings.push(
      `⚠️ Client 目录中使用了 ET.Server 命名空间。Client 代码应使用 ET.Client 命名空间。`
    );
  }

  // Server 目录中不应有 ET.Client 命名空间
  if (filePath.includes("/Server/") && content.includes("namespace ET.Client")) {
    warnings.push(
      `⚠️ Server 目录中使用了 ET.Client 命名空间。Server 代码应使用 ET.Server 命名空间。`
    );
  }
}

if (warnings.length > 0) {
  process.stdout.write(
    JSON.stringify({
      decision: "allow",
      reason: `QX Guard 检测到潜在的程序集分离问题:\n${warnings.join("\n")}\n\n这是启发式检查，请确认文件放置正确。编译期 Source Generator 会做最终验证。`,
    })
  );
} else {
  process.stdout.write(JSON.stringify({ decision: "allow" }));
}

process.exit(0);

// --- 工具函数 ---

async function readStdin() {
  const chunks = [];
  for await (const chunk of process.stdin) {
    chunks.push(chunk);
  }
  return Buffer.concat(chunks).toString("utf8");
}
