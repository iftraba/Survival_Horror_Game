# Sonidos del juego: encargo para fal.ai

Modelo de efectos: `cassetteai/sound-effects-generator` (máx. 30 s). Música: `fal-ai/stable-audio-25/text-to-audio`.
Se generan con `generate_audio` (acción `generate`, luego `status`) a `Assets/_Project/Audio/Generated/`. Cada clip
sustituye a su equivalente sintetizado (mismo hueco en `GameAudio`) cuando el usuario lo da por bueno.

| Nombre | Sustituye a | Duración (s) | Prompt (inglés) |
|---|---|---|---|
| pistol_shot | pistol_shot | 2 | single 9mm handgun gunshot indoors, sharp crack with short room reverb tail |
| shotgun_shot | shotgun_shot | 3 | 12 gauge pump shotgun blast indoors, deep boom with echo |
| shotgun_pump | (nuevo: recarga por cartucho) | 1.5 | pump action shotgun racking, metallic clack |
| reload_pistol | reload | 2 | pistol magazine ejected and inserted, metallic clicks |
| reload_shotgun | reload | 3 | inserting shotgun shells one by one into a pump shotgun, metallic clicks |
| dry_fire | dry_fire | 1 | empty pistol hammer click |
| door_open | door_open | 2.5 | old wooden door creaking open slowly |
| door_close | door_close | 1.5 | wooden door closing with a thud and latch click |
| door_locked | door_locked | 1.5 | locked door handle rattling |
| door_unlock | door_unlock | 1.5 | key turning in a lock, metallic click |
| zgroan_1..4 | zgroan1..4 | 2.5 | male zombie groan, low guttural moan, horror |
| zattack | zattack | 1.5 | zombie snarling attack growl, short |
| zhurt_1..2 | (zombieHurts) | 1.2 | zombie pained grunt, short |
| zdeath | zdeath | 2.5 | zombie dying gurgle and body falling to the floor |
| boss_roar | (nuevo: despertar del jefe) | 4 | giant monster roar, deep and distorted, horror boss |
| boss_step | (nuevo) | 1.5 | heavy footstep of a giant creature, deep thud |
| player_hurt | player_hurt | 1 | man grunting in pain, short |
| player_death | player_death | 3 | man dying gasp and falling |
| step_tile_1..3 | step1..3 | 0.6 | single boot footstep on a tile floor, indoors |
| step_stairs_1..2 | (nuevo: escalera) | 0.6 | single boot footstep on concrete stairs, indoors |
| phone_ring | (nuevo: teléfono) | 4 | old rotary telephone ringing, mechanical bell |
| phone_dial | (nuevo: usar el teléfono) | 2.5 | rotary telephone dial turning back with ratchet clicks |
| phone_pickup | (nuevo) | 2.5 | old telephone handset lifted and put down, plastic clunk |
| pickup | pickup | 1 | picking up a small object, light rustle |
| flashlight | flashlight | 0.5 | flashlight switch click |
| switch | switch | 0.7 | heavy wall light switch clunk |
| lamp_zap | lamp_zap | 1.5 | fluorescent tube flickering buzz and pop |
| lamp_hum | lamp_hum | 6 | fluorescent lamp electrical hum, steady loop |
| heartbeat | heartbeat | 4 | slow human heartbeat, deep thump, loop |
| sting_creak | sting_creak | 3 | distant wooden creak in an empty building, horror |
| sting_bang | sting_bang | 2 | distant metal bang in an empty building, horror |
| sting_scrape | sting_scrape | 3 | distant metal scraping on concrete, horror |
| music_ambient | music_ambient | 60 | dark ambient horror drone, low and tense, sparse, no melody (stable-audio) |
| music_tension | music_tension | 45 | horror tension music, pulsing low strings, dissonant, urgent (stable-audio) |

Notas
- Probar primero 2-3 clips (disparo, gruñido, teléfono) y juzgar antes de lanzar el resto.
- Los bucles (`lamp_hum`, `heartbeat`, música) hay que recortarlos para que enlacen; revisar `loop` al importar.
- La clave de fal.ai se guarda en el almacén seguro del editor (no se pega en el chat).
