# Voz de Chispa (asistente de seguridad)

Chispa (`MascotGuide.cs`) dice estas frases. Si existe un audio con el nombre de la clave en
`unity/Assets/Loto/Resources/Voice/`, lo reproduce; si no, muestra el texto con un sonido corto.

**Cómo grabarlas:** con un modelo de voz local (por ejemplo voicebox), una voz cálida y clara en español, ritmo
tranquilo. Exportar **WAV o OGG mono**, con el nombre exacto de la clave (por ejemplo `intro.wav`, `hint_PRENSA.wav`,
`intro_NEUMÁTICA.wav`). Copiarlos a esa carpeta, volver a Unity y armar el APK (no hace falta Build Scene).

| Clave (nombre del archivo) | Frase |
|---|---|
| `intro` | ¡Hola! Soy Chispa. Antes de tocar una máquina: apagar, candado, verificar y recién tocar. |
| `intro_MOLINO` | Este molino tiene una polea pesada. Ojo: sigue girando un rato después de apagarlo. |
| `intro_PRENSA` | Esta prensa guarda presión en el aceite. Apagarla no alcanza: hay que purgar. |
| `intro_NEUMÁTICA` | Esta estación guarda aire comprimido. Apagar no alcanza: hay que purgar. |
| `ok1` | Bien, palanca abajo. Ahora tu candado en el punto de bloqueo. |
| `ok2` | Candado puesto. Nadie puede encenderla. Ahora verifica. |
| `ok3` | No arranca: quedó sin energía. Ahora sí puedes tocarla. |
| `done` | ¡Excelente! Secuencia completa. Así se trabaja seguro. |
| `hint` | Espera: todavía hay energía guardada. Mira la máquina antes de verificar. |
| `hint_PRENSA` | Todavía hay presión. Toca PURGAR y espera que el manómetro marque cero. |
| `hint_NEUMÁTICA` | Todavía hay aire en la línea. Toca PURGAR y espera el cero. |
| `fail` | ¡Alto! Te saltaste un paso. Mira el panel: te marca cuál. |
| `warn` | Cuidado, ese paso no va todavía. Revisa el orden. |

La explicación que escribe la IA del servidor (`ENCARGO-IA-SERVIDOR.md`) aparece en el globo de Chispa como texto.
