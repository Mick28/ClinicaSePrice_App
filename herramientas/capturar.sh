#!/usr/bin/env bash
# Capturas de pantalla con Mono y Xvfb (verificación visual en Linux).
# Uso: herramientas/capturar.sh "mruiz:P-01,P-27;lgomez:P-01"
cd "$(dirname "$0")/../bin_mono"
rm -f SePrice.db
pgrep -x Xvfb >/dev/null || (Xvfb :77 -screen 0 1600x1000x24 >/dev/null 2>&1 &)
sleep 1
rm -rf /tmp/claude-0/capturas
LANG=C.UTF-8 LC_ALL=C.UTF-8 SEPRICE_CAPTURA="$1" DISPLAY=:77 timeout 280 mono SePrice.exe > /tmp/claude-0/mono.log 2>&1
ls /tmp/claude-0/capturas | wc -l
