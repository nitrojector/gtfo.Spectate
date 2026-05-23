#!/usr/bin/env bash
./rename.py
echo "Setting up virtual environment and dependencies..."
python3 -m venv de
./de/bin/pip3 install cairosvg Pillow
