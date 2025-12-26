# Quick Start: Getting Ollama Working

## ?? Choose Your Setup

### Option A: Local Ollama (Same Machine) - 5 Minutes
[Skip to Local Setup](#local-setup-same-machine)

### Option B: Remote Ollama (Network Server) - 10 Minutes  
[Skip to Remote Setup](#remote-setup-network-server)

---

## Local Setup (Same Machine)

### Step 1: Install Ollama (if not already)
Download from: **https://ollama.ai/download**

Verify installation:
```bash
ollama --version
```

---

### Step 2: Start Ollama
Open PowerShell or Terminal and run:
```bash
ollama serve
```

**Important:** Keep this terminal window open while using the extension.

**Windows Note:** Ollama usually installs as a service and starts automatically. You can skip this step if Ollama is already running as a service.

---

### Step 3: Pull a Model
Open a **new** terminal (keep `ollama serve` running) and run:
```bash
ollama pull codellama
```

This downloads the CodeLlama model (~2-3 GB). Other good options:
```bash
ollama pull llama3          # General chat, 4GB
ollama pull deepseek-coder  # Excellent for code, 18GB
```

---

### Step 4: Verify Ollama is Working
Test the connection:
```bash
curl http://localhost:11434/api/tags
```

Or run our diagnostic:
```powershell
.\diagnose-ollama.ps1
```

You should see your installed model(s) listed.

---

### Step 5: Use the Extension

1. **Open Visual Studio 2022**
2. **Press `Ctrl+Shift+O`** to open Ollama Copilot
3. **Click the "?" button** to refresh models
4. **Select "codellama:latest"** from the dropdown
5. **Type a message** and press Send

**Example first message:**
```
What is a singleton pattern?
```

**? Local setup complete!**

---

## Remote Setup (Network Server)

### Prerequisites
- Ollama installed on a Linux/Mac server on your network
- Network access between your Windows machine and the server

---

### Step 1: Configure Ollama on Your Server

**SSH into your Linux server** and run:

```bash
# Set Ollama to listen on all network interfaces (not just localhost)
export OLLAMA_HOST=0.0.0.0:11434

# Add to ~/.bashrc or ~/.zshrc to make permanent
echo 'export OLLAMA_HOST=0.0.0.0:11434' >> ~/.bashrc
source ~/.bashrc

# Start Ollama
ollama serve
```

**Keep this terminal open** or set up as a system service.

---

### Step 2: Configure Firewall on Server

Allow incoming connections on port 11434:

**Ubuntu/Debian:**
```bash
sudo ufw allow 11434/tcp
sudo ufw reload
```

**CentOS/RHEL/Fedora:**
```bash
sudo firewall-cmd --permanent --add-port=11434/tcp
sudo firewall-cmd --reload
```

---

### Step 3: Pull Models on Server

```bash
# Pull recommended models
ollama pull codellama       # Best for code, ~2.5GB
ollama pull llama3          # General chat, ~4GB
ollama pull deepseek-coder  # Excellent for code, ~18GB

# Verify models
ollama list
```

---

### Step 4: Get Your Server's IP Address

On your Linux server:
```bash
hostname -I
# or
ip addr show
```

**Example output:** `192.168.1.100`  
?? **Note this IP address** - you'll need it for the extension.

---

### Step 5: Test Connection from Windows

On your **Windows development machine**, open PowerShell:

```powershell
# Replace 192.168.1.100 with your server's IP
curl http://192.168.1.100:11434/api/tags

# Or use PowerShell cmdlet
Invoke-WebRequest -Uri "http://192.168.1.100:11434/api/tags"
```

**Expected:** JSON response with list of models

**If this fails,** see [Troubleshooting Remote Connections](#troubleshooting-remote)

---

### Step 6: Configure Extension for Remote Server

1. **Open Visual Studio 2022**
2. **Press `Ctrl+Shift+O`** to open Ollama Copilot
3. **Click the "? Settings" button**
4. **Change "Server:" field to:**
   ```
   http://192.168.1.100:11434
   ```
   *(Replace `192.168.1.100` with your actual server IP)*
5. **Click "? Refresh Models" button**
6. **Models from your server should now appear**

---

### Step 7: Use the Extension

**Select a model** from the dropdown and start chatting!

**Example first message:**
```
Explain how async/await works in C#
```

**? Remote setup complete!**

---

## ? Verification Checklist

### For Local Setup:
- [ ] Ollama is installed (`ollama --version` works)
- [ ] Ollama is running (`ollama serve` or Windows service)
- [ ] At least one model is downloaded (`ollama list` shows models)
- [ ] Connection works (`curl http://localhost:11434/api/tags` returns JSON)
- [ ] Extension can see models (model dropdown is not empty)

### For Remote Setup:
- [ ] Ollama is installed on server (`ollama --version`)
- [ ] Ollama configured for network (`OLLAMA_HOST=0.0.0.0:11434`)
- [ ] Ollama is running on server
- [ ] Firewall allows port 11434 on server
- [ ] Models downloaded on server (`ollama list`)
- [ ] Can ping server from Windows (`ping YOUR_SERVER_IP`)
- [ ] Can access API from Windows (`curl http://YOUR_SERVER_IP:11434/api/tags`)
- [ ] Extension server setting updated to server IP
- [ ] Extension can see models

---

## ?? Troubleshooting

### Problem: "No models found" (Local)

**Quick Fix:**
1. Open PowerShell
2. Run: `ollama serve` (keep it running)
3. Open another PowerShell
4. Run: `ollama pull codellama`
5. In VS, click the "? Refresh Models" button

---

### <a name="troubleshooting-remote"></a>Problem: Can't connect to remote server

**Quick Diagnostic Steps:**

1. **Verify server is reachable:**
   ```powershell
   ping 192.168.1.100
   ```
   Should get replies. If not, check network connection.

2. **Check if Ollama is listening on network:**
   ```bash
   # On Linux server
   sudo ss -tlnp | grep 11434
   ```
   Should show `0.0.0.0:11434` (not `127.0.0.1:11434`)
   
   **If showing 127.0.0.1 only:**
   ```bash
   export OLLAMA_HOST=0.0.0.0:11434
   ollama serve
   ```

3. **Check firewall on server:**
   ```bash
   sudo ufw status
   # If port 11434 not listed:
   sudo ufw allow 11434/tcp
   ```

4. **Test from Windows:**
   ```powershell
   curl http://192.168.1.100:11434/api/tags
   ```
   Should return JSON with models.

**Still not working?** See detailed guide: [REMOTE_OLLAMA_SETUP.md](REMOTE_OLLAMA_SETUP.md)

---

### Problem: Connection fails (Local)

**Quick Fix:**
```powershell
# Run diagnostic
.\diagnose-ollama.ps1

# If Ollama not running:
ollama serve

# If no models:
ollama pull codellama
```

---

### Problem: Extension can't connect but Ollama is running

**Check server address:**
1. In the extension, click "? Settings"
2. Verify "Server:" field:
   - **Local:** `http://localhost:11434`
   - **Remote:** `http://YOUR_SERVER_IP:11434`
3. Click "? Refresh Models"

**Common addresses:**
- `http://localhost:11434` (local, default)
- `http://127.0.0.1:11434` (local, alternative)
- `http://192.168.1.100:11434` (remote server on network)
- `http://192.168.0.50:11434` (remote, different subnet)

---

## ?? More Help

- **Remote server setup:** See [REMOTE_OLLAMA_SETUP.md](REMOTE_OLLAMA_SETUP.md) (detailed guide)
- **Full troubleshooting:** See [OLLAMA_TROUBLESHOOTING.md](OLLAMA_TROUBLESHOOTING.md)
- **Extension features:** See [PROJECT_COMPLETE.md](PROJECT_COMPLETE.md)
- **Ollama docs:** https://ollama.ai/docs

---

## ?? Ready to Code!

Once you see models in the dropdown, you're ready to use AI assistance:

**Keyboard Shortcuts:**
- `Ctrl+Shift+O` - Open Ollama Copilot
- `Ctrl+Shift+E` - Explain selected code
- `Ctrl+Shift+R` - Refactor selected code
- `Ctrl+Shift+I` - Find issues in code

**Try this:**
1. Select some code in your editor
2. Press `Ctrl+Shift+E`
3. Get an AI explanation!

---

## ?? Setup Comparison

| Feature | Local Setup | Remote Setup |
|---------|------------|--------------|
| **Setup Time** | 5 minutes | 10 minutes |
| **Resource Usage** | Uses local CPU/RAM | Uses server CPU/RAM |
| **Performance** | Fast (no network) | Depends on network |
| **Advantages** | Simple, no network | Powerful server, GPU support |
| **Use When** | Laptop/desktop with good specs | Limited local resources, shared team server |

---

**Need more help?** Run `.\diagnose-ollama.ps1` for detailed diagnostics or see [REMOTE_OLLAMA_SETUP.md](REMOTE_OLLAMA_SETUP.md) for remote server configuration.
