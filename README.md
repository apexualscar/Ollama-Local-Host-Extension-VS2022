# 🧠 Ollama Local Host Extension for Visual Studio Community 2022

Bring the power of local LLMs into your Visual Studio workflow with this extension! Designed for developers who value privacy, speed, and seamless integration, this tool connects your local Ollama server to VSCommunity2022, enabling AI-assisted coding without sending data to the cloud.

## 🚀 Features

- **Local LLM Integration**: Connects to your running Ollama instance for fast, secure AI responses.
- **Contextual Code Assistance**: Select code blocks and get suggestions, refactors, or explanations.
- **Prompt Console**: Send custom prompts directly to your model from within Visual Studio.
- **Model Switching**: Easily swap between supported models (e.g., `llama3`, `mistral`, `phi3`, etc.).
- **Quick Actions**:
  - 🧹 Remove duplicate lines
  - ✏️ Modify & replicate code with prompt-based substitutions
  - 🧼 Erase or add text to selected code blocks

## 🛠️ Installation

1. **Install Ollama**  
   Download and install Ollama from [ollama.com/download](https://ollama.com/download).  
   Start the server:  
   ```bash
   ollama run llama3

2. **Clone This Repo**
git clone https://github.com/your-username/Ollama-Local-Host-Extension-VS2022.git

3. **Open in Visual Studio 2022**
Open the .sln file and build the extension.

4. **Enable the Extension**
Go to Tools > Extensions and Updates and enable the Ollama extension.
