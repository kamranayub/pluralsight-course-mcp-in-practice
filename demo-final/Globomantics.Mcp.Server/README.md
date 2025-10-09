## Examples

### Ping Server

```json
{"jsonrpc": "2.0", "id": 1, "method": "ping", "params": {} }
```

### Call Echo Tool

(Paste as one-liner)

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "echo",
    "arguments": {
      "message": "Hello, Tools!"
    }
  }
}
```

