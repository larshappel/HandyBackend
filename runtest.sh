#!/usr/bin/env bash
set -euo pipefail

dotnet test HandyBackend.Tests/HandyBackend.Tests.csproj "$@"
