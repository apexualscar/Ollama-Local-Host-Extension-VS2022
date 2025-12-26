# Remote Ollama Server Setup Guide

## Overview

This guide covers connecting the Visual Studio extension to an Ollama server running on a **remote machine** (like a Linux server on your local network) instead of localhost.

---

## ?? Remote Server Setup (Your Linux Machine)

### Step 1: Install Ollama on Linux Server

On your Linux machine:
```bash
# Install Ollama
curl -fsSL https://ollama.ai/install.sh | sh

# Verify installation
ollama --version
```

### Step 2: Configure Ollama to Accept Network Connections

By default, Ollama only listens on `localhost`. To accept connections from other machines on your network:

**Option A: Using Environment Variable (Recommended)**
```bash
# Edit your shell profile (~/.bashrc or ~/.zshrc)
export OLLAMA_HOST=0.0.0.0:11434

# Apply changes
source ~/.bashrc

# Start Ollama
ollama serve
```

**Option B: Using systemd service (if installed as service)**
```bash
# Edit the service file
sudo systemctl edit ollama.service

# Add these lines:
[Service]
Environment="OLLAMA_HOST=0.0.0.0:11434"

# Reload and restart
sudo systemctl daemon-reload
sudo systemctl restart ollama
```

**Option C: Direct command line**
```bash
# Start Ollama with network binding
OLLAMA_HOST=0.0.0.0:11434 ollama serve
```

### Step 3: Configure Firewall on Linux Server

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

### Step 4: Pull Models on Linux Server

```bash
# Pull recommended models
ollama pull codellama       # Best for code, ~2.5GB
ollama pull llama3          # General chat, ~4GB
ollama pull deepseek-coder  # Excellent for code, ~18GB

# Verify models
ollama list
```

### Step 5: Test Server is Accessible

From your Linux server:
```bash
# Test locally
curl http://localhost:11434/api/tags

# Test from network interface
curl http://0.0.0.0:11434/api/tags
```

---

## ?? Windows Client Setup (Your Development Machine)

### Step 1: Find Your Linux Server's IP Address

On your Linux server, run:
```bash
# Get IP address
hostname -I
# or
ip addr show

# Example output: 192.168.1.100
```

### Step 2: Test Connection from Windows

On your Windows development machine:
```powershell
# Test connection (replace with your server's IP)
curl http://192.168.1.100:11434/api/tags

# Or use PowerShell
Invoke-WebRequest -Uri "http://192.168.1.100:11434/api/tags"
```

**Expected output:** JSON with list of models

### Step 3: Configure Visual Studio Extension

1. **Open Visual Studio 2022**
2. **Press `Ctrl+Shift+O`** to open Ollama Copilot
3. **Click the "? Settings" button**
4. **Change "Server:" to your Linux server's address**
   ```
   http://192.168.1.100:11434
   ```
   *(Replace 192.168.1.100 with your actual server IP)*
5. **Click "? Refresh Models"**
6. **Models should now appear in the dropdown**

---

## ?? Network Configuration Examples

### Example 1: Local Network (Most Common)

**Linux Server IP:** `192.168.1.100`  
**Extension Setting:** `http://192.168.1.100:11434`

```
[Windows PC]  ???????  [Linux Server]
192.168.1.50          192.168.1.100:11434
```

### Example 2: Custom Port

If you need to use a different port:

**On Linux:**
```bash
export OLLAMA_HOST=0.0.0.0:8080
ollama serve
```

**Extension Setting:** `http://192.168.1.100:8080`

### Example 3: Using Hostname

If you have DNS or hosts file configured:

**Extension Setting:** `http://mylinuxserver.local:11434`

Or on Windows, edit `C:\Windows\System32\drivers\etc\hosts`:
```
192.168.1.100    mylinuxserver
```
**Extension Setting:** `http://mylinuxserver:11434`

---

## ??? Troubleshooting Remote Connections

### Problem 1: "Connection Refused" or Timeout

**Possible Causes:**
1. Firewall blocking port 11434
2. Ollama not configured to accept network connections
3. Wrong IP address

**Solutions:**

**Check if Ollama is listening on network interface:**
```bash
# On Linux server
sudo ss -tlnp | grep 11434
# or
sudo netstat -tlnp | grep 11434
```

Should show `0.0.0.0:11434` not `127.0.0.1:11434`

**If showing 127.0.0.1 only:**
```bash
# Ollama is only listening on localhost
# Set OLLAMA_HOST environment variable
export OLLAMA_HOST=0.0.0.0:11434
ollama serve
```

**Check firewall on Linux:**
```bash
# Ubuntu/Debian
sudo ufw status
sudo ufw allow 11434/tcp

# CentOS/RHEL
sudo firewall-cmd --list-ports
sudo firewall-cmd --permanent --add-port=11434/tcp
sudo firewall-cmd --reload
```

**Check firewall on Windows:**
- Windows Defender Firewall should allow **outgoing** connections by default
- If using third-party firewall, allow outgoing to port 11434

### Problem 2: "No Models Found" but Connection Works

**Cause:** Models not installed on server

**Solution:** On Linux server:
```bash
ollama list             # Check installed models
ollama pull codellama   # Pull models
```

### Problem 3: Slow Response Times

**Possible Causes:**
- Network latency
- Server underpowered
- Large model on limited RAM

**Solutions:**
- Use smaller models (llama3:8b instead of llama3:70b)
- Check network speed: `ping 192.168.1.100`
- Monitor server resources: `htop` or `top`

### Problem 4: Certificate/SSL Issues

If using HTTPS (reverse proxy setup):

**With Self-Signed Certificate:**
Extension may reject self-signed certificates. Use HTTP for local network.

**With Valid Certificate:**
```
Extension Setting: https://ollama.yourdomain.com
```

---

## ?? Advanced: Reverse Proxy Setup (Optional)

### Using Nginx on Linux Server

**Install Nginx:**
```bash
sudo apt install nginx  # Ubuntu/Debian
sudo yum install nginx  # CentOS/RHEL
```

**Configure `/etc/nginx/sites-available/ollama`:**
```nginx
server {
    listen 80;
    server_name ollama.local;

    location / {
        proxy_pass http://127.0.0.1:11434;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        
        # Important for streaming
        proxy_buffering off;
        proxy_cache off;
    }
}
```

**Enable and restart:**
```bash
sudo ln -s /etc/nginx/sites-available/ollama /etc/nginx/sites-enabled/
sudo nginx -t
sudo systemctl restart nginx
```

**Extension Setting:** `http://192.168.1.100` (port 80)

---

## ?? Performance Considerations

### Network Requirements
- **Minimum:** 100 Mbps Ethernet or WiFi
- **Recommended:** 1 Gbps Ethernet
- **Latency:** < 5ms for best experience

### Server Requirements
- **CPU:** 4+ cores recommended
- **RAM:** 8GB minimum, 16GB+ recommended
- **GPU:** Optional but significantly faster (NVIDIA with CUDA)

### Model Selection for Network Use
- **Fast:** llama3:8b, codellama:13b
- **Balanced:** llama3:13b, codellama:34b
- **Quality:** mixtral:8x7b, deepseek-coder:33b

---

## ? Verification Checklist

### On Linux Server:
- [ ] Ollama installed (`ollama --version`)
- [ ] Ollama configured to listen on 0.0.0.0 (`OLLAMA_HOST=0.0.0.0:11434`)
- [ ] Ollama running (`systemctl status ollama` or check with `ps aux | grep ollama`)
- [ ] Firewall allows port 11434 (`sudo ufw status`)
- [ ] Models downloaded (`ollama list`)
- [ ] Accessible from network (`curl http://192.168.1.100:11434/api/tags` from Windows)

### On Windows Development Machine:
- [ ] Can ping Linux server (`ping 192.168.1.100`)
- [ ] Can curl Ollama API (`curl http://192.168.1.100:11434/api/tags`)
- [ ] Extension server setting updated to `http://192.168.1.100:11434`
- [ ] Models appear in extension dropdown
- [ ] Can send messages and get responses

---

## ?? Security Considerations

### Local Network Only (Recommended)
If Ollama is only on your local network (192.168.x.x or 10.x.x.x):
- ? Safe for most home/office networks
- ? No authentication needed
- ? Firewalled from internet

### Exposed to Internet (Not Recommended)
If you must expose Ollama to the internet:
- ?? Add authentication (reverse proxy with basic auth)
- ?? Use HTTPS with valid certificate
- ?? Implement rate limiting
- ?? Monitor for abuse

**Example with Nginx Basic Auth:**
```nginx
location / {
    auth_basic "Ollama Server";
    auth_basic_user_file /etc/nginx/.htpasswd;
    proxy_pass http://127.0.0.1:11434;
}
```

---

## ?? Quick Reference

### Linux Server Commands
```bash
# Start Ollama with network access
OLLAMA_HOST=0.0.0.0:11434 ollama serve

# Check if listening on network
sudo ss -tlnp | grep 11434

# Allow firewall
sudo ufw allow 11434/tcp

# Pull models
ollama pull codellama

# Check models
ollama list
```

### Windows Client Commands
```powershell
# Test connection
curl http://192.168.1.100:11434/api/tags

# Or PowerShell
Invoke-WebRequest -Uri "http://192.168.1.100:11434/api/tags"

# Run diagnostic
.\diagnose-ollama.ps1
```

### Extension Settings
```
Server: http://192.168.1.100:11434
```
*(Replace with your server's IP)*

---

## ?? Summary

**For Remote Ollama on Linux:**

1. **On Linux Server:**
   ```bash
   export OLLAMA_HOST=0.0.0.0:11434
   ollama serve
   sudo ufw allow 11434/tcp
   ollama pull codellama
   ```

2. **On Windows (VS Extension):**
   - Open Settings (?)
   - Set Server: `http://YOUR_LINUX_IP:11434`
   - Refresh Models (?)

3. **Test:**
   ```powershell
   curl http://YOUR_LINUX_IP:11434/api/tags
   ```

**That's it!** No API keys, no complex configuration needed. ??
