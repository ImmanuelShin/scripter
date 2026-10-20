#include <stdio.h>
#include "script_manager.h"

int save_script(const char *filename, const Script *script) {
    printf("[ScriptManager] Saving script: %s\n", filename);
    // TODO: Implement actual file writing logic
    return 1;
}

int load_script(const char *filename, Script *script) {
    printf("[ScriptManager] Loading script: %s\n", filename);
    // TODO: Implement actual file reading logic
    return 1;
}
