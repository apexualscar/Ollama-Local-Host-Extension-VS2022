# 🤖 Ollama Copilot - Visual Studio 2022 Extension

**A complete GitHub Copilot alternative powered by Ollama's local LLM models.** Code with AI assistance while keeping your data private and secure on your own infrastructure.

[![Visual Studio](https://img.shields.io/badge/Visual%20Studio-2022-purple.svg)](https://visualstudio.microsoft.com/)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-blue.svg)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

---

## ✨ Features

### 🎯 Dual Mode System
- **Ask Mode** - Get explanations, best practices, and code reviews
- **Agent Mode** - Apply code changes directly to your editor with diff preview

### 💬 Intelligent Chat Interface
- GitHub Copilot-style UI with rich code block display
- Automatic code block extraction from AI responses
- Copy code snippets with one click
- Apply code changes with visual diff preview
- Conversation history with persistent context
- Token usage monitoring with color-coded warnings

### 🔧 Context Menu Integration
Right-click on any code to:
- **Explain Code** - Get detailed explanations
- **Refactor with Ollama** - Receive refactoring suggestions
- **Find Issues** - Analyze for bugs and improvements

### ⌨️ Keyboard Shortcuts
- `Ctrl+Shift+O` - Open Ollama Copilot window
- `Ctrl+Shift+E` - Explain selected code
- `Ctrl+Shift+R` - Refactor selected code
- `Ctrl+Shift+I` - Find issues in code

### 📊 Advanced Features
- **Diff Preview Dialog** - See changes before applying (side-by-side & unified views)
- **Token Counter** - Real-time tracking with warnings for context limits
- **Multiple Model Support** - Switch between codellama, llama3, deepseek-coder, and more
- **Code Context Extraction** - Automatically includes relevant code in prompts
- **6 Specialized AI Methods** - Optimized prompts for different tasks

---

## 🚀 Quick Start

### Prerequisites
- Visual Studio 2022 (Community, Professional, or Enterprise)
- Ollama installed and running
- At least one Ollama model downloaded

### Installation

#### Option 1: Install from VSIX (Recommended)
1. Download the latest `.vsix` file from [Releases](https://github.com/your-username/ollama-copilot-vs/releases)
2. Double-click the `.vsix` file to install
3. Restart Visual Studio

#### Option 2: Build from Source
```bash
# Clone the repository
git clone https://github.com/your-username/ollama-copilot-vs.git
cd ollama-copilot-vs

# Open in Visual Studio 2022
start OllamaLocalHostIntergration.sln

# Press F5 to build and debug
```

---

## ⚙️ Ollama Setup

### Local Setup (Same Machine)

#### 1. Install Ollama
Download from: **https://ollama.ai/download**

Verify installation:
```bash
ollama --version
```

#### 2. Start Ollama
```bash
ollama serve
```

**Windows Note:** Ollama typically installs as a service and auto-starts.

#### 3. Pull Models
```bash
# For code assistance (recommended)
ollama pull codellama       # 2.5GB - Best for code
ollama pull llama3          # 4GB - Good for explanations
ollama pull deepseek-coder  # 18GB - Excellent for code

# Verify installation
ollama list
```

#### 4. Configure Extension
1. Open Visual Studio 2022
2. Press `Ctrl+Shift+O` to open Ollama Copilot
3. Click the "↻" button to refresh models
4. Select a model from the dropdown
5. Start coding with AI!

---

### Remote Setup (Network Server)

#### On Your Server (Linux/Mac)

**1. Configure Ollama for Network Access**
```bash
# Set Ollama to listen on all network interfaces
export OLLAMA_HOST=0.0.0.0:11434

# Make permanent by adding to ~/.bashrc or ~/.zshrc
echo 'export OLLAMA_HOST=0.0.0.0:11434' >> ~/.bashrc
source ~/.bashrc
```

**2. Start Ollama**
```bash
ollama serve
```

Or set up as systemd service:
```bash
sudo systemctl edit ollama.service

# Add:
[Service]
Environment="OLLAMA_HOST=0.0.0.0:11434"

# Reload and restart
sudo systemctl daemon-reload
sudo systemctl restart ollama
```

**3. Configure Firewall**
```bash
# Ubuntu/Debian
sudo ufw allow 11434/tcp
sudo ufw reload

# CentOS/RHEL/Fedora
sudo firewall-cmd --permanent --add-port=11434/tcp
sudo firewall-cmd --reload
```

**4. Pull Models**
```bash
ollama pull codellama
ollama pull llama3
ollama list  # Verify
```

**5. Get Server IP Address**
```bash
hostname -I
# Example output: 192.168.1.100
```

#### On Your Development Machine (Windows)

**1. Test Connection**
```powershell
# Test connectivity (replace with your server's IP)
curl http://192.168.1.100:11434/api/tags

# Or run diagnostic
.\diagnose-ollama.ps1 -ServerAddress 192.168.1.100
```

**2. Configure Extension**
1. Open Visual Studio 2022
2. Press `Ctrl+Shift+O` to open Ollama Copilot
3. Click the "⚙ Settings" button
4. Set Server: `http://192.168.1.100:11434`
5. Click "↻ Refresh Models"
6. Models should now appear!

---

## 📖 Usage Guide

### Basic Chat
1. Press `Ctrl+Shift+O` to open the tool window
2. Type your question in the input box
3. Press `Send` or `Enter`
4. View the AI response with formatted code blocks

### Explain Code
1. Select code in the editor
2. Press `Ctrl+Shift+E` or right-click → "Explain Code"
3. Read the explanation in the chat window

### Refactor Code
1. Select code to refactor
2. Press `Ctrl+Shift+R` or right-click → "Refactor with Ollama"
3. Review the suggested changes
4. Click "Apply to Editor" button
5. Preview changes in the diff dialog
6. Click "Apply Changes" to update your code

### Find Issues
1. Select code to analyze
2. Press `Ctrl+Shift+I` or right-click → "Find Issues"
3. Review the analysis for bugs, security issues, and improvements

### Ask Mode vs Agent Mode
- **Ask Mode** - For questions, explanations, and learning (read-only)
- **Agent Mode** - For code modifications with apply functionality
- Switch modes using the dropdown in the toolbar

---

## 🎨 Features in Detail

### Rich Message Display
- **Automatic Code Block Detection** - Markdown code blocks are parsed and styled
- **Language-Specific Headers** - Each code block shows its language
- **Copy Buttons** - One-click copy for any code block
- **Apply Buttons** - Agent mode responses can be applied directly

### Diff Preview Dialog
- **Side-by-Side View** - Compare original vs modified code
- **Unified Diff View** - Git-style diff with +/- indicators
- **Change Statistics** - See lines added, removed, and modified
- **Apply/Cancel** - Review before committing changes

### Token Management
- **Real-Time Counter** - Monitor token usage as you chat
- **Color-Coded Warnings** - Visual feedback when approaching limits
  - Green: < 4000 tokens (safe)
  - Orange: 4000-6000 tokens (caution)
  - Red: > 6000 tokens (approaching limit)
- **Context + History** - Includes both code context and conversation

### Context Menu Commands
All commands automatically:
- Open the tool window if not already open
- Switch to the appropriate mode (Ask/Agent)
- Include the selected code as context
- Generate optimized prompts for the task

---

## 🔧 Configuration

### Settings Panel
Access via the "⚙" button in the tool window:

- **Server Address** - Ollama server URL (default: `http://localhost:11434`)
- **Code Context** - View current code context and token count
- **Refresh Context** - Update context from active document

### Supported Models

| Model | Size | Best For | Speed |
|-------|------|----------|-------|
| **codellama:7b** | 3.8GB | Code generation | Fast |
| **codellama:13b** | 7.3GB | Code + explanations | Balanced |
| **codellama:34b** | 19GB | Complex code tasks | Slower |
| **llama3:8b** | 4.7GB | General chat | Fast |
| **llama3:70b** | 39GB | High-quality responses | Slow |
| **deepseek-coder:6.7b** | 3.8GB | Code generation | Fast |
| **deepseek-coder:33b** | 18GB | Professional coding | Balanced |
| **mixtral:8x7b** | 26GB | Complex reasoning | Slow |

---

## 🛠️ Troubleshooting

### Can't Connect to Ollama?

**Run the diagnostic:**
```powershell
# Local
.\diagnose-ollama.ps1

# Remote
.\diagnose-ollama.ps1 -ServerAddress 192.168.1.100
```

### Common Issues

#### "No models found"
```bash
# Make sure Ollama is running
ollama serve

# Pull at least one model
ollama pull codellama

# Refresh in extension
# Click the "↻" button
```

#### "Connection refused" (Remote)
```bash
# On server: Check if listening on network
sudo ss -tlnp | grep 11434
# Should show: 0.0.0.0:11434 (not 127.0.0.1:11434)

# If wrong, set environment variable:
export OLLAMA_HOST=0.0.0.0:11434
ollama serve
```

#### "Connection timeout"
```bash
# Check firewall on server
sudo ufw status
sudo ufw allow 11434/tcp

# Test from Windows
ping YOUR_SERVER_IP
curl http://YOUR_SERVER_IP:11434/api/tags
```

### More Help
- **Quick Start:** See [QUICK_START.md](QUICK_START.md)
- **Remote Setup:** See [REMOTE_OLLAMA_SETUP.md](REMOTE_OLLAMA_SETUP.md)
- **Troubleshooting:** See [OLLAMA_TROUBLESHOOTING.md](OLLAMA_TROUBLESHOOTING.md)

---

## 📊 Architecture

### Components

**Services:**
- `OllamaService` - HTTP client for Ollama API
- `CodeEditorService` - Visual Studio text editor integration
- `CodeModificationService` - AI response parsing and code application
- `MessageParserService` - Code block extraction from markdown
- `PromptBuilder` - Intelligent prompt engineering
- `ModeManager` - Ask/Agent mode management

**UI Components:**
- `MyToolWindowControl` - Main chat interface
- `DiffPreviewDialog` - Side-by-side code comparison
- `RichChatMessageControl` - Enhanced message display (future)

**Commands:**
- `MyToolWindowCommand` - Open window
- `ExplainCodeCommand` - Explain selected code
- `RefactorCodeCommand` - Refactor code
- `FindIssuesCommand` - Find bugs and issues

---

## 🔒 Privacy & Security

### Local-First Approach
- **No Cloud Services** - All AI processing happens on your Ollama server
- **No Data Transmission** - Code never leaves your network
- **No Tracking** - No telemetry or usage data collected
- **Open Source** - Transparent, auditable code

### Network Security
- **Local Network Only** - Recommended for home/office LANs
- **No Authentication by Default** - Suitable for trusted networks
- **Firewall Protected** - Should not be exposed to internet
- **Optional HTTPS** - Can be configured with reverse proxy

### For Production Use
If exposing Ollama to internet:
- Use reverse proxy with authentication
- Enable HTTPS with valid certificates
- Implement rate limiting
- Monitor for abuse

See [REMOTE_OLLAMA_SETUP.md](REMOTE_OLLAMA_SETUP.md) for secure configurations.

---

## 🎯 Roadmap

### Current Version (1.0)
- ✅ Dual mode system (Ask/Agent)
- ✅ Rich chat interface with code blocks
- ✅ Context menu integration
- ✅ Keyboard shortcuts
- ✅ Diff preview dialog
- ✅ Token management
- ✅ Remote server support

### Future Enhancements
- [ ] Conversation history persistence
- [ ] Export conversations
- [ ] Custom prompt templates
- [ ] Multi-file context awareness
- [ ] Inline code suggestions
- [ ] Streaming responses
- [ ] Response caching
- [ ] Solution-wide analysis
- [ ] Team collaboration features

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit issues, fork the repository, and create pull requests.

### Development Setup
1. Clone the repository
2. Open `OllamaLocalHostIntergration.sln` in Visual Studio 2022
3. Restore NuGet packages
4. Press F5 to build and debug
5. Experimental instance of VS will launch

### Code Style
- Follow C# coding conventions
- Use meaningful variable names
- Add XML documentation for public methods
- Keep methods focused and single-purpose

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- **Ollama Team** - For the amazing local LLM platform
- **Visual Studio Community Toolkit** - For VS extension development tools
- **CodeLlama, Llama3, DeepSeek** - For excellent open-source models

---

## 📞 Support

- **Issues:** [GitHub Issues](https://github.com/your-username/ollama-copilot-vs/issues)
- **Documentation:** See the `/Documentation` folder
- **Ollama Docs:** https://ollama.ai/docs

---

## ⭐ Show Your Support

If you find this extension useful, please consider:
- ⭐ Starring the repository
- 🐛 Reporting bugs
- 💡 Suggesting features
- 📖 Improving documentation
- 🔀 Contributing code

---

**Built with ❤️ for the Visual Studio community**

*Bringing the power of local AI to developers everywhere* 🚀
