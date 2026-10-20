#include <stdio.h>
#include "gui.h"
#include "script_manager.h"

int main(int argc, char *argv[]) {
    printf("Starting Macro App...\n");
    
    if (!init_gui(argc, argv)) {
        fprintf(stderr, "Failed to initialize GUI.\n");
        return 1;
    }

    run_gui();
    return 0;
}