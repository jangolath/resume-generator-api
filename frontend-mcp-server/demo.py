import asyncio
from mcp import (
    ToolCallParams, ResourceRequestParams, 
    MCPServiceType, MCPConnector
)

async def get_react_docs():
    """Demo function to get React documentation from the MCP server."""
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
        print("Retrieved React Documentation for Essential Knowledge:")
        print(result.result)
        
    except Exception as e:
        print(f"Error: {e}")
    finally:
        # Close the connection
        await connector.close()

if __name__ == "__main__":
    asyncio.run(get_react_docs())