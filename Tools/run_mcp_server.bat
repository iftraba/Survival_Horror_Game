@echo off
rem Servidor MCP de Unity (puerto 8080). Dejar esta ventana abierta mientras se trabaja con Claude.
rem Si cambia la version del paquete en Unity, ajustar mcpforunityserver==10.3.0.
echo Arrancando servidor MCP de Unity en http://127.0.0.1:8080 ...
"%USERPROFILE%\.local\bin\uvx.exe" --from mcpforunityserver==10.3.0 python "%~dp0run_mcp_server.py"
echo.
echo El servidor se ha detenido. Pulsa una tecla para cerrar.
pause >nul
