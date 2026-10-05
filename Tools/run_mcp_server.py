"""Arranca el servidor MCP de Unity sin usar el .exe lanzador de uv.

Windows (Smart App Control / App Control) bloquea ese .exe sin firmar con el error 4551, y el boton
"Start Server" de Unity falla. Ejecutarlo con Python, que si esta permitido, hace exactamente lo mismo.

Uso (lo hace run_mcp_server.bat):
    uvx --from mcpforunityserver==10.3.0 python run_mcp_server.py
"""
import sys

sys.argv = ["mcp-for-unity", "--transport", "http", "--http-url", "http://127.0.0.1:8080", "--project-scoped-tools"]

from main import main  # noqa: E402  (modulo del paquete mcpforunityserver)

main()
