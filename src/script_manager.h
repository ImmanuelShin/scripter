#ifndef SCRIPT_MANAGER_H
#define SCRIPT_MANAGER_H

typedef enum {
    ACTION_KEY_PRESS,
    ACTION_MOUSE_CLICK,
    ACTION_WAIT,
    ACTION_MOVE_MOUSE
} ActionType;

typedef struct {
    ActionType type;
    int param1;
    int param2;
} Action;

typedef struct {
    char name[64];
    Action *actions;
    int action_count;
    int repeat_mode;
} Script;

int save_script(const char *filename, const Script *script);
int load_script(const char *filename, Script *script);

#endif // SCRIPT_MANAGER_H