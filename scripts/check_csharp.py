"""Smoke-test the compiled C# example against real local HTTP and a fake provider."""

import json
import os
import shutil
import socket
import subprocess
import sys
import threading
import time
from datetime import date
from pathlib import Path
from unittest.mock import Mock, patch

import requests
import uvicorn

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))

from api import create_app
from service import FlightService
from settings import Settings


def main() -> None:
    dotnet = shutil.which("dotnet")
    if dotnet is None:
        raise SystemExit("The .NET 8 SDK or newer is required for this check.")
    subprocess.run([dotnet, "build", str(ROOT / "examples/csharp/FlightSearchExample.csproj"),
                    "--configuration", "Release", "--nologo"], check=True)
    settings = Settings(travelpayouts_token="smoke-provider-placeholder",
                        internal_api_key="smoke-internal-placeholder-" + "c" * 32)
    service = FlightService(settings, today=lambda: date(2026, 11, 7))
    application = create_app(settings, service=service)
    listener = socket.socket()
    listener.bind(("127.0.0.1", 0))
    port = listener.getsockname()[1]
    server = uvicorn.Server(uvicorn.Config(application, log_level="critical", access_log=False))
    thread = threading.Thread(target=server.run, kwargs={"sockets": [listener]}, daemon=True)
    response = Mock(spec=requests.Response)
    response.status_code = 200
    tickets = json.loads((ROOT / "examples/provider_offers.json").read_text(encoding="utf-8"))
    dll = ROOT / "examples/csharp/bin/Release/net8.0/FlightSearchExample.dll"
    child_env = {**os.environ, "INTERNAL_API_KEY": settings.internal_api_key,
                 "FLIGHTS_BASE_URL": f"http://127.0.0.1:{port}/"}
    try:
        with patch("travelpayouts.requests.get", return_value=response):
            thread.start()
            deadline = time.monotonic() + 8
            while not server.started:
                if not thread.is_alive() or time.monotonic() > deadline:
                    raise RuntimeError("Local server did not start")
                time.sleep(.05)
            cases = [
                ("success", tickets, settings.internal_api_key, "2026-12-15", 0, "offers: 2"),
                ("empty", [], settings.internal_api_key, "2026-12-15", 0, "offers: 0"),
                ("invalid_key", [], "wrong-key", "2026-12-15", 1, "UNAUTHORIZED"),
                ("invalid_date", [], settings.internal_api_key, "2026-11-06", 1, "INVALID_SEARCH_PARAMETERS"),
            ]
            for name, payload, key, departure, expected_code, expected_text in cases:
                response.json.return_value = {"success": True, "data": payload}
                child_env["INTERNAL_API_KEY"] = key
                result = subprocess.run([dotnet, str(dll), departure], env=child_env,
                                        capture_output=True, text=True, timeout=15)
                if result.returncode != expected_code or expected_text not in result.stdout + result.stderr:
                    raise RuntimeError("C# integration case failed: " + name)
                print(name + ": passed")
    finally:
        server.should_exit = True
        if thread.ident is not None:
            thread.join(timeout=5)
        listener.close()


if __name__ == "__main__":
    main()
