#!/usr/bin/env -S uv run --script
# /// script
# requires-python = ">=3.8"
# dependencies = []
# ///

"""
Lightweight hook script to send Claude Code events to the observability server.
No external dependencies required - uses only stdlib.
"""

import json
import sys
import argparse
import urllib.request
import urllib.error
from datetime import datetime


def send_event(event_data, server_url="http://localhost:4000/events"):
    try:
        req = urllib.request.Request(
            server_url,
            data=json.dumps(event_data).encode("utf-8"),
            headers={
                "Content-Type": "application/json",
                "User-Agent": "Claude-Code-Hook/1.0",
            },
        )
        with urllib.request.urlopen(req, timeout=5) as resp:
            return resp.status == 200
    except Exception:
        return False


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-app", required=True)
    parser.add_argument("--event-type", required=True)
    parser.add_argument("--server-url", default="http://localhost:4000/events")
    parser.add_argument("--add-chat", action="store_true")
    args = parser.parse_args()

    try:
        input_data = json.load(sys.stdin)
    except json.JSONDecodeError:
        sys.exit(0)

    event_data = {
        "source_app": args.source_app,
        "session_id": input_data.get("session_id", "unknown"),
        "hook_event_type": args.event_type,
        "payload": input_data,
        "timestamp": int(datetime.now().timestamp() * 1000),
    }

    # Forward well-known fields as top-level for easier querying
    for field in [
        "tool_name", "tool_use_id", "error", "is_interrupt",
        "agent_id", "agent_type", "agent_transcript_path",
        "stop_hook_active", "notification_type", "source", "reason",
    ]:
        if field in input_data:
            event_data[field] = input_data[field]

    # Optionally attach chat transcript
    if args.add_chat and "transcript_path" in input_data:
        import os
        tp = input_data["transcript_path"]
        if os.path.exists(tp):
            chat = []
            try:
                with open(tp, "r") as f:
                    for line in f:
                        line = line.strip()
                        if line:
                            try:
                                chat.append(json.loads(line))
                            except json.JSONDecodeError:
                                pass
                event_data["chat"] = chat
            except Exception:
                pass

    send_event(event_data, args.server_url)
    sys.exit(0)


if __name__ == "__main__":
    main()
