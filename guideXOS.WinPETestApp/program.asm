; program.asm - ML64 syntax
PUBLIC my_entry

TEXT SEGMENT
my_entry PROC
    xor rax, rax   ; exit code
    ret
my_entry ENDP
TEXT ENDS

END
