# macro_app/Makefile

# Compiler and flags
CC = gcc
CFLAGS = -Wall -Wextra -g

# Project settings
TARGET = scripter.exe
SRC_DIR = src
SRCS = $(SRC_DIR)/main.c $(SRC_DIR)/gui.c $(SRC_DIR)/script_manager.c
OBJS = $(SRCS:.c=.o)

# Default target
all: $(TARGET)

$(TARGET): $(OBJS)
	$(CC) $(OBJS) -o $(TARGET)

$(SRC_DIR)/%.o: $(SRC_DIR)/%.c
	$(CC) $(CFLAGS) -c $< -o $@

clean:
	del /Q $(SRC_DIR)\\*.o $(TARGET)
