# Frontend MCP Server Setup and Usage Guide

This document explains how the Frontend MCP Server was set up and how to use its capabilities.

## Installation Steps Completed

1. Created directory for the MCP server:
   ```
   mkdir frontend-mcp-server
   ```

2. Installed prerequisites:
   ```powershell
   # Install uv package manager
   Invoke-WebRequest -Uri "https://astral.sh/uv/install.ps1" -OutFile "$env:TEMP\uv-install.ps1"
   Set-ExecutionPolicy -ExecutionPolicy Bypass -Scope Process -Force
   & "$env:TEMP\uv-install.ps1"
   
   # Add uv to PATH
   $env:Path = "C:\Users\jwood\.local\bin;$env:Path"
   
   # Install Python 3.10
   uv python install 3.10
   ```

3. Created MCP configuration:
   ```json
   {
     "mcpServers": {
       "github.com/awslabs/mcp/tree/main/src/frontend-mcp-server": {
         "command": "uvx",
         "args": ["awslabs.frontend-mcp-server@latest"],
         "env": {
           "FASTMCP_LOG_LEVEL": "ERROR"
         },
         "disabled": false,
         "autoApprove": []
       }
     }
   }
   ```

## Server Capabilities

The Frontend MCP Server provides specialized tools for modern web application development through its `GetReactDocsByTopic` tool. This tool offers comprehensive documentation on:

- **Essential Knowledge**: Fundamental concepts for building React applications
- **Basic UI Setup**: Setting up a React project with Tailwind CSS and shadcn/ui
- **Authentication**: AWS Amplify authentication integration
- **Routing**: Implementing routing with React Router
- **Customizing**: Theming with AWS Amplify components
- **Creating Components**: Building React components with AWS integrations
- **Troubleshooting**: Common issues and solutions for React development

## Using the Server

A demo script has been created to show how to interact with the MCP server:

```python
import asyncio
from mcp import (
    ToolCallParams, ResourceRequestParams, 
    MCPServiceType, MCPConnector
)

async def get_react_docs():
    # Connect to the MCP server
    connector = MCPConnector(
        server_name="github.com/awslabs/mcp/tree/main/src/frontend-mcp-server",
        service_type=MCPServiceType.stdio
    )
    
    try:
        # Initialize connection to the server
        await connector.connect()
        
        # Get React documentation for essential knowledge
        result = await connector.tool_call(
            ToolCallParams(
                tool_name="GetReactDocsByTopic",
                arguments={"topic": "essential-knowledge"}
            )
        )
        
        # Print the results
        print("Retrieved React Documentation:")
        print(result.result)
        
    finally:
        # Close the connection
        await connector.close()

if __name__ == "__main__":
    asyncio.run(get_react_docs())
```

## Sample Topics

You can request documentation on any of these topics:

1. `essential-knowledge`: Foundational concepts for React with AWS
2. `troubleshooting`: Common issues and solutions for React development

## Integration with AI Assistants

The Frontend MCP Server is designed to integrate with AI assistants, providing them with access to specialized documentation on modern web application development techniques. This enables the AI to provide more accurate and context-specific guidance when helping with React application development tasks.

## Next Steps

To test the MCP server directly, run:

```powershell
$env:Path = "C:\Users\jwood\.local\bin;$env:Path"
uvx awslabs.frontend-mcp-server@latest
```

And then in a separate terminal, run the demo script:

```powershell
python frontend-mcp-server/demo.py