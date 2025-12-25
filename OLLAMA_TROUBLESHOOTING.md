# Ollama Connection Troubleshooting Guide

## Problem: "Can't find my Ollama" / Connection Failed

The extension connects to Ollama via HTTP at `http://localhost:11434` by default. **No special API configuration is needed** - Ollama just needs to be running.

---

## Quick Fix (Most Common)

### Step 1: Verify Ollama is Installed
Open PowerShell or Command Prompt and run:
```bash
ollama --version
```

If this fails, install Ollama from: https://ollama.ai/download

### Step 2: Start Ollama Server
```bash
ollama serve
```

This starts the Ollama server on `http://localhost:11434`

### Step 3: Pull a Model
```bash
ollama pull codellama
# or
ollama pull llama3
```

### Step 4: Test Connection
Run the diagnostic script:
```powershell
.\diagnose-ollama.ps1
```

Or test manually:
```bash
curl http://localhost:11434/api/tags
```

---

## Detailed Troubleshooting

### Issue 1: Ollama is Not Running

**Symptoms:**
- Extension shows "No models found"
- Status bar says "Failed to fetch models"
- Connection timeout errors

**Solution:**
1. Open PowerShell/Terminal
2. Run: `ollama serve`
3. Leave this terminal open (Ollama must keep running)
4. In VS, click the "? Refresh Models" button

**Alternative (Windows Service):**
Ollama usually installs as a Windows service and auto-starts. Check:
1. Press `Win + R`
2. Type: `services.msc`
3. Look for "Ollama" service
4. Ensure it's running and set to "Automatic"

---

### Issue 2: Firewall Blocking Port 11434

**Symptoms:**
- "Connection refused" error
- Timeout when trying to connect

**Solution:**
1. Open Windows Defender Firewall
2. Click "Advanced settings"
3. Add inbound rule for port 11434
4. Allow TCP connections

**PowerShell command (Admin required):**
```powershell
New-NetFirewallRule -DisplayName "Ollama" -Direction Inbound -LocalPort 11434 -Protocol TCP -Action Allow
```

---

### Issue 3: Different Port or Address

**Symptoms:**
- Ollama is running but extension can't connect

**Check Ollama's actual address:**
```bash
# Windows
netstat -ano | findstr :11434

# See what's listening on port 11434
```

**If Ollama is on a different address:**
1. In the extension, click the "? Settings" button
2. Change "Server:" to your Ollama address
3. Examples:
   - `http://127.0.0.1:11434`
   - `http://192.168.1.100:11434` (remote server)
   - `http://localhost:8080` (custom port)

---

### Issue 4: Environment Variable Override

Ollama respects the `OLLAMA_HOST` environment variable.

**Check if set:**
```bash
echo %OLLAMA_HOST%        # Windows CMD
$env:OLLAMA_HOST          # PowerShell
```

**If set to something other than localhost:11434:**
Use that address in the extension settings.

**Example:**
```
OLLAMA_HOST=http://192.168.1.50:11434
```
Then in VS extension settings, set server to: `http://192.168.1.50:11434`

---

### Issue 5: No Models Pulled

**Symptoms:**
- Extension connects successfully
- Model dropdown is empty
- Status bar says "No models found"

**Solution:**
```bash
# Pull recommended models
ollama pull codellama      # Best for code
ollama pull llama3         # Good for general chat
ollama pull deepseek-coder # Excellent for code

# Verify models are installed
ollama list
```

---

### Issue 6: CORS or Network Issues

**Symptoms:**
- Works in browser but not in VS extension
- "Network error" messages

**Solution:**
Ollama's API should work without CORS configuration, but if issues persist:

1. Restart Ollama:
```bash
# Stop
taskkill /F /IM ollama.exe

# Start
ollama serve
```

2. Check if antivirus is blocking:
   - Temporarily disable antivirus
   - Test connection
   - Add ollama.exe to exclusions

---

## Testing Ollama Connection Manually

### Test 1: Check if Server is Responding
```bash
curl http://localhost:11434/api/tags
```

**Expected output:**
```json
{
  "models": [
    {
      "name": "codellama:latest",
      ...
    }
  ]
}
```

### Test 2: Send a Test Message
```bash
curl http://localhost:11434/api/chat -d '{
  "model": "codellama",
  "messages": [
    {"role": "user", "content": "Hello"}
  ]
}'
```

**Expected:** JSON response with AI message

---

## Common Error Messages

### "Failed to fetch models: Unable to connect to the remote server"
**Cause:** Ollama is not running  
**Fix:** Run `ollama serve`

### "Failed to fetch models: The operation has timed out"
**Cause:** Firewall blocking or wrong address  
**Fix:** Check firewall, verify address

### "No models found. Make sure Ollama is running."
**Cause:** No models installed  
**Fix:** Run `ollama pull codellama`

### "Error: No response generated"
**Cause:** Model failed to load or crashed  
**Fix:** Check Ollama logs, try different model

---

## Extension Settings Configuration

In Visual Studio:
1. Open the Ollama Copilot tool window (`Ctrl+Shift+O`)
2. Click the "? Settings" button
3. Update "Server:" field

**Default:** `http://localhost:11434`

**Custom Examples:**
- Remote server: `http://192.168.1.100:11434`
- Custom port: `http://localhost:8080`
- HTTPS (if configured): `https://ollama.example.com`

**Note:** The extension automatically saves the server address. No restart needed.

---

## Verify Extension is Working

1. **Open Ollama Copilot**: Press `Ctrl+Shift+O`
2. **Click Refresh Models**: The "?" button
3. **Check model dropdown**: Should show available models
4. **Select a model**: e.g., "codellama:latest"
5. **Send a test message**: Type "Hello" and press Send
6. **Check for response**: Should get AI reply

---

## Still Not Working?

### Run Full Diagnostic:
```powershell
.\diagnose-ollama.ps1
```

### Check Ollama Logs:
**Windows:**
```powershell
# If running as service
Get-EventLog -LogName Application -Source Ollama -Newest 50

# If running manually, check the terminal where you ran "ollama serve"
```

**Linux/Mac:**
```bash
journalctl -u ollama -n 50
```

### Get Help:
1. Check Ollama GitHub: https://github.com/ollama/ollama/issues
2. Ollama Discord: https://discord.gg/ollama
3. Extension Issues: (your repo URL)

---

## Quick Reference Commands

```bash
# Check if Ollama is installed
ollama --version

# Start Ollama server
ollama serve

# List installed models
ollama list

# Pull a model
ollama pull codellama
ollama pull llama3

# Test Ollama API
curl http://localhost:11434/api/tags

# Check running processes
tasklist | findstr ollama        # Windows
ps aux | grep ollama             # Linux/Mac

# Check port usage
netstat -ano | findstr :11434    # Windows
lsof -i :11434                   # Linux/Mac
```

---

## Summary

**Ollama requires NO special API configuration.** It just needs to:
1. ? Be installed (`ollama --version`)
2. ? Be running (`ollama serve` or Windows service)
3. ? Have at least one model (`ollama pull codellama`)
4. ? Be accessible at `http://localhost:11434`

If all the above are true and you still can't connect, run `diagnose-ollama.ps1` and check the firewall.
