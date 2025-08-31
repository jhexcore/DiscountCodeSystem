# Discount Code System

## Overview
Server generates and validates single-use DISCOUNT codes communicated over **raw TCP sockets**.  
Codes saved to `codes.json`.

## Features 
- Random, **unique** codes of length **7–8** (A–Z, 2–9; avoids ambiguous chars).
- **Generate** up to **2000** codes per request.
- **Use** a code exactly once.
- **Persistence** to JSON file (`codes.json`) in the server's folder.
- **Parallel clients** handled via async sockets.
- **client** console app to interact with the server.

## Protocol (TCP, line-delimited)
- `GENERATE <count> [7|8]` → returns comma-separated list of new codes  
- `USE <code>` → returns success or error message  
- `EXIT` → closes client connection

Example session:
```
$ dotnet run --project DiscountServer
Connected to Discount Server. Commands: GENERATE <count>  [7|8] | USE <code> | EXIT

# In another terminal
$ dotnet run --project DiscountClient
> GENERATE 3
9P4QF2K, 6H5ZB57C, BT78KQ4R
> GENERATE 1 7
2XF269G
> USE 9P4QF2K
SUCCESS: Code 9P4QF2K used
> USE 9P4QF2K
ERROR: Code already used
> EXIT
```

## How to run
1. **Prereqs**: .NET 8 SDK (`dotnet --version`).
2. Start server (default port 6001):
   ```bash
   dotnet run --project DiscountServer
   ```
   Or choose a port:
   ```bash
   dotnet run --project DiscountServer -- 6001
   ```
3. Run client:
   ```bash
   dotnet run --project DiscountClient
   ```
   Or connect to custom host/port:
   ```bash
   dotnet run --project DiscountClient -- 127.0.0.1 6001
   ```

## Design decisions
- **TCP sockets**: Simple, dependency-free protocol per instructions.
- **Concurrency**: Each client handled in a separate Task; shared state guarded with a `lock`.
- **Uniqueness & speed**: `HashSet<string>` for O(1) uniqueness checks.
- **Persistence**: JSON file saved atomically (write to `.tmp` then replace) to reduce corruption risk.
- **Code alphabet**: Excludes `I`, `O`, `1`, `0` to reduce confusion; still 30 chars → plenty of entropy.
- **Input validation**: Clear error messages for malformed commands.
- **GENERATE Response**: Returns string type boolean as part 1 of the response for easy to convert it as Boolean type and part 2 to is the list of codes generated if successful.

## Notes & possible extensions
- Swap JSON for a database (SQLite/Postgres) if you need multi-instance servers.
- Add authentication or TLS if running across networks.
- Add additional commands (e.g., list remaining/used counts).

## Projects
```
DiscountCodeSystem/
 ├─ DiscountServer/      # TCP server, persists codes.json
 ├─ DiscountClient/      # Simple interactive TCP client
```