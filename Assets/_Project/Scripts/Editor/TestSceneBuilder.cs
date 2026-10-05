#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Horror.EditorTools
{
    public static class TestSceneBuilder
    {
        const string Data = "Assets/_Project/Data";
        const string Mats = "Assets/_Project/Materials";

        [MenuItem("Horror/Build Test Scene")]
        public static void Build()
        {
            Directory.CreateDirectory(Data);
            Directory.CreateDirectory(Mats);
            AssetDatabase.Refresh();

            // ---------- Datos ----------
            var pistol = Asset<WeaponData>("W_Pistol", w =>
            {
                w.displayName = "Pistola M19"; w.ammoType = AmmoType.Handgun; w.damage = 28f; w.fireRate = 3f;
                w.magazineSize = 12; w.reloadTime = 1.6f; w.spread = 0.4f; w.range = 45f;
            });
            var shotgun = Asset<WeaponData>("W_Shotgun", w =>
            {
                w.displayName = "Escopeta"; w.ammoType = AmmoType.Shotgun; w.damage = 14f; w.pellets = 8;
                w.spread = 4f; w.fireRate = 1f; w.magazineSize = 6; w.reloadTime = 2.4f; w.range = 20f;
            });
            var pistolItem = Asset<ItemData>("I_Pistol", i =>
            {
                i.displayName = "Pistola M19"; i.type = ItemType.Weapon; i.weapon = pistol; i.tint = Color.gray;
                i.description = "Pistola semiautomatica. Cargador de 12 balas.";
            });
            var shotgunItem = Asset<ItemData>("I_Shotgun", i =>
            {
                i.displayName = "Escopeta"; i.type = ItemType.Weapon; i.weapon = shotgun; i.tint = new Color(0.5f, 0.3f, 0.1f);
                i.description = "Gran dano a corta distancia.";
            });
            var handgunAmmo = Asset<ItemData>("I_HandgunAmmo", i =>
            {
                i.displayName = "Municion 9mm"; i.type = ItemType.Ammo; i.ammoType = AmmoType.Handgun; i.maxStack = 30;
                i.tint = Color.yellow; i.description = "Municion para la pistola.";
            });
            var shotgunAmmo = Asset<ItemData>("I_ShotgunAmmo", i =>
            {
                i.displayName = "Cartuchos"; i.type = ItemType.Ammo; i.ammoType = AmmoType.Shotgun; i.maxStack = 12;
                i.tint = Color.red; i.description = "Cartuchos de escopeta.";
            });
            var spray = Asset<ItemData>("I_Spray", i =>
            {
                i.displayName = "Spray curativo"; i.type = ItemType.Healing; i.healAmount = 60f; i.tint = Color.green;
                i.description = "Restaura salud.";
            });
            var key = Asset<ItemData>("I_KeyRoom", i =>
            {
                i.displayName = "Llave de la sala"; i.type = ItemType.Key; i.tint = Color.cyan;
                i.description = "Abre la puerta de la sala segura.";
            });
            var keyExit = Asset<ItemData>("I_KeyExit", i =>
            {
                i.displayName = "Llave de la salida"; i.type = ItemType.Key; i.tint = new Color(1f, 0.5f, 0.1f);
                i.description = "Abre la puerta de salida del edificio.";
            });
            // Objetivos que se muestran al avanzar (se reaplican en cada construccion)
            key.pickupObjective = "Llave conseguida. Abre la puerta de la sala del fondo.";
            keyExit.pickupObjective = "Tienes la llave de salida. Ve a la puerta de la pared norte, al fondo.";
            EditorUtility.SetDirty(key);
            EditorUtility.SetDirty(keyExit);

            // ---------- Materiales ----------
            // Materiales con textura (si faltan, se usan los planos de reserva)
            var floorMat = Env("Env_Floor", Mat("Floor", new Color(0.18f, 0.18f, 0.2f)));
            var wallMat = Env("Env_Wall", Mat("Wall", new Color(0.3f, 0.28f, 0.26f)));
            var doorMat = Env("Env_Wood", Mat("Door", new Color(0.35f, 0.2f, 0.1f)));
            var woodMat = Env("Env_Wood", Mat("Door", new Color(0.35f, 0.2f, 0.1f)));
            var ceilingMat = Env("Env_Ceiling", wallMat);
            var metalMat = Env("Env_Metal", Mat("Wall", new Color(0.3f, 0.3f, 0.32f)));
            var playerMat = Mat("Player", new Color(0.2f, 0.35f, 0.7f));
            var zombieMat = Mat("Zombie", new Color(0.35f, 0.5f, 0.3f));

            // ---------- Escena ----------
            // Limpia una ejecucion anterior para poder regenerar sin duplicados
            foreach (var n in new[] { "--- LEVEL ---", "Items", "Player", "HUD", "Zombies", "GameAudio", "Details", "GameFlow" })
            {
                var old = GameObject.Find(n);
                if (old != null) Object.DestroyImmediate(old);
            }
            var root = new GameObject("--- LEVEL ---");
            // Adornos (frisos, tuberias, vigas, decales): fuera del horneado del NavMesh y sin colisiones
            var details = new GameObject("Details").transform;
            var ground = Box("Ground", new Vector3(0, -0.5f, 0), new Vector3(30, 1, 40), floorMat, root.transform, 4f);   // la textura del suelo cubre 4 m (baldosas de 0.5 m)
            // La superficie vive en la raiz del nivel y solo recoge su geometria (no personajes ni objetos)
            var surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            var items = new GameObject("Items").transform;

            // Perimetro (sala principal z -15..5, sala segura z 5..15)
            Box("Wall_N", new Vector3(0, 1.5f, 20), new Vector3(30, 3, 1), wallMat, root.transform, 3f);
            Box("Wall_S", new Vector3(0, 1.5f, -20), new Vector3(30, 3, 1), wallMat, root.transform, 3f);
            Box("Wall_E", new Vector3(15, 1.5f, 0), new Vector3(1, 3, 40), wallMat, root.transform, 3f);
            Box("Wall_W", new Vector3(-15, 1.5f, 0), new Vector3(1, 3, 40), wallMat, root.transform, 3f);
            // Pared interior con hueco central de 2m para la puerta
            Box("Wall_Mid_L", new Vector3(-8, 1.5f, 5), new Vector3(14, 3, 1), wallMat, root.transform, 3f);
            Box("Wall_Mid_R", new Vector3(8, 1.5f, 5), new Vector3(14, 3, 1), wallMat, root.transform, 3f);
            // Dintel a 2.4 m (centro 2.7, alto 0.6): el jugador (capsula de 2 m) debe caber por debajo de la puerta
            Box("Wall_Mid_Top", new Vector3(0, 2.7f, 5), new Vector3(2, 0.6f, 1), wallMat, root.transform, 3f);

            // Puerta de la sala del fondo: doble hoja metalica reforzada, con llave (los zombis no pueden forzarla)
            DoorLeaves.Clear();
            var door = MakeDoor(root.transform, new Vector3(-1f, 0f, 5f), 0f, 1.0f, metalMat, metalMat, true, "Door_Main");
            door.requiredKey = key;
            door.openedObjective = "Registra la sala del fondo y busca la llave de la salida.";
            var door2 = MakeDoor(root.transform, new Vector3(1f, 0f, 5f), 180f, 1.0f, metalMat, metalMat, true, "Door_Main_R");
            door2.requiredKey = key;
            door2.consumeKey = false;
            door.partner = door2;
            door2.partner = door;

            // ---------- Iluminacion ----------
            var (mainLamps, backLamps) = BuildLighting(root.transform, ceilingMat);

            // Un interruptor por estancia, junto a su entrada (1.3 m de altura). Vestuario, almacen y despacho
            // empiezan a oscuras: hay que encontrar el interruptor (o moverse con la poca luz ambiente)
            WallSwitch(root.transform, new Vector3(1.5f, 1.3f, -13.115f), 0f, new[] { mainLamps[0] }).startOn = true;                         // vestibulo
            WallSwitch(root.transform, new Vector3(1.5f, 1.3f, -12.885f), 180f, new[] { mainLamps[3], mainLamps[4], mainLamps[5], mainLamps[6], mainLamps[7] }).startOn = true;   // oficina
            WallSwitch(root.transform, new Vector3(-5.115f, 1.3f, -15.75f), 90f, new[] { mainLamps[1] }).startOn = false;                     // vestuario
            WallSwitch(root.transform, new Vector3(11.3f, 1.3f, -13.115f), 0f, new[] { mainLamps[2] }).startOn = false;                       // almacen
            WallSwitch(root.transform, new Vector3(-8.115f, 1.3f, 2.3f), 90f, new[] { mainLamps[8] }).startOn = false;                        // despacho
            WallSwitch(root.transform, new Vector3(2.0f, 1.3f, 5.515f), 180f, backLamps).startOn = true;                                      // sala del fondo

            // Acabados de arquitectura, atrezzo y manchas de sangre
            BuildArchitecture(root.transform, details, wallMat, woodMat, metalMat);
            BuildPoliceStation(root.transform, details, wallMat, woodMat, metalMat);
            BuildDecals(details);
            BuildAtmosphere(details);
            BuildExitAndTerminals(root.transform, keyExit, metalMat);

            // Objetos del suelo: cada uno con su modelo y fisicas (se dejan caer desde poca altura)
            SetWorld(pistolItem, "Pistol", 1.4f, 1.0f);
            // Si existe la pistola generada con IA (ya reducida y con texturas horneadas), sustituye a la procedural
            var aiPistol = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Generated/MeshyPistolLow.fbx");
            if (aiPistol != null)
            {
                pistolItem.worldPrefab = aiPistol;
                pistolItem.worldScale = 1.3f;
                pistol.heldPrefab = aiPistol;
                EditorUtility.SetDirty(pistolItem);
                EditorUtility.SetDirty(pistol);
            }
            SetWorld(shotgunItem, "Shotgun", 0.8f, 3.2f);
            SetWorld(handgunAmmo, "AmmoBox", 1.5f, 0.4f);
            SetWorld(shotgunAmmo, "ShellBox", 1.5f, 0.5f);
            SetWorld(spray, "Spray", 1.4f, 0.4f);
            SetWorld(key, "Key", 1.6f, 0.1f);
            SetWorld(keyExit, "Key", 1.6f, 0.1f);
            // Objetos pequenos generados con IA (si existen)
            void UseAi(ItemData it, string file, float scale)
            {
                var ai = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Generated/" + file + ".fbx");
                if (ai == null) return;
                it.worldPrefab = ai;
                it.worldScale = scale;
                EditorUtility.SetDirty(it);
            }
            UseAi(handgunAmmo, "MeshyAmmoBoxLow", 1.5f);
            UseAi(shotgunAmmo, "MeshyShellBoxLow", 1.5f);
            UseAi(spray, "MeshySprayLow", 1.4f);
            // Igual con la escopeta generada con IA (si existe)
            var aiShotgun = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Generated/MeshyShotgunLow.fbx");
            if (aiShotgun != null)
            {
                shotgunItem.worldPrefab = aiShotgun;
                shotgunItem.worldScale = 0.9f;
                shotgun.heldPrefab = aiShotgun;
                shotgun.heldScale = 0.9f;
                EditorUtility.SetDirty(shotgunItem);
                EditorUtility.SetDirty(shotgun);
            }
            // Cada objeto en un sitio con sentido: mostrador, taquillas abiertas, estanterias y mesas (caen y se asientan)
            // Miniaturas del inventario a partir de los modelos definitivos
            ItemIcons.Generate(pistolItem, shotgunItem, handgunAmmo, shotgunAmmo, spray, key, keyExit);
            // La pistola no es botin: se empieza con ella equipada (GameFlow.startWeapon)
            Pick(handgunAmmo, 12, new Vector3(-3.0f, 0.95f, -13.6f), items);    // mostrador de recepcion
            Pick(handgunAmmo, 15, new Vector3(-12.15f, 1.45f, -19.2f), items);  // balda de una taquilla (vestuario)
            Pick(spray, 1, new Vector3(-9.45f, 1.45f, -19.2f), items);          // otra taquilla abierta
            Pick(key, 1, new Vector3(11.4f, 1.2f, -16.2f), items);              // balda central de una estanteria del almacen
            Pick(shotgunItem, 1, new Vector3(-7.8f, 0.95f, 19.1f), items);      // mesa de la armeria (sala del fondo)
            Pick(shotgunAmmo, 8, new Vector3(14.2f, 1.45f, 12.0f), items);     // taquilla abierta de la sala del fondo
            Pick(keyExit, 1, new Vector3(8.7f, 1.0f, 19.0f), items);            // escritorio de la sala del fondo
            Pick(handgunAmmo, 10, new Vector3(-7.65f, 0.3f, -19.2f), items);    // fondo de la ultima taquilla del vestuario
            Pick(spray, 1, new Vector3(-11.5f, 0.95f, 1.0f), items);            // mesa del despacho del capitan
            Pick(handgunAmmo, 8, new Vector3(5.6f, 0.95f, -6.5f), items);       // puesto de trabajo de la oficina
            Pick(spray, 1, new Vector3(-11.2f, 0.95f, -10.5f), items);          // otro puesto de la oficina
            Pick(handgunAmmo, 10, new Vector3(12.7f, 1.2f, -16.2f), items);     // estanteria del almacen
            Pick(spray, 1, new Vector3(-13.5f, 0.75f, 17f), items);             // catre de la sala del fondo
            Pick(handgunAmmo, 12, new Vector3(9.4f, 1.0f, 19.0f), items);       // escritorio de la sala del fondo

            // ---------- Jugador ----------
            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = new Vector3(0, 1.01f, -17);
            // Protagonista generado con IA (mismo esqueleto: usa las animaciones de Player.fbx); si falta, el de bloques
            string playerFbx = AssetDatabase.LoadAssetAtPath<GameObject>(CharPath + "PlayerGen.fbx") != null ? "PlayerGen.fbx" : "Player.fbx";
            var playerModel = AttachModel(player.transform, CharPath + playerFbx, AnimPath + "PlayerAnimator.controller");
            SetupPlayerAnimator();
            var handHolder = CreateHandHolder(playerModel, "Aim", "WeaponHolder");
            var longHolder = CreateHandHolder(playerModel, "Aim_Long", "LongWeaponHolder");

            var cc = player.AddComponent<CharacterController>();
            cc.height = 2f; cc.radius = 0.4f; cc.center = Vector3.zero;
            var pc = player.AddComponent<PlayerController>();
            player.AddComponent<Health>();
            player.AddComponent<PlayerFootsteps>();
            player.AddComponent<PlayerAudio>();
            var inv = player.AddComponent<Inventory>();
            var wc = player.AddComponent<WeaponController>();
            player.AddComponent<PlayerInteractor>();
            player.AddComponent<PlayerAnimation>();

            var camGo = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            var tpc = camGo.GetComponent<ThirdPersonCamera>();
            if (tpc == null) tpc = camGo.AddComponent<ThirdPersonCamera>();
            tpc.pivotHeight = 0.7f;
            tpc.target = player.transform;
            tpc.player = pc;
            pc.cam = tpc;
            wc.handSocket = handHolder;
            wc.longSocket = longHolder;
            var pAnim = player.GetComponent<PlayerAnimation>();
            pAnim.handgunController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimPath + "PlayerAnimator.controller");
            pAnim.longGunController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimPath + "PlayerAnimator_Long.overrideController");
            shotgun.twoHanded = true;
            pistol.twoHanded = false;
            wc.aimCamera = camGo.GetComponent<Camera>();
            wc.player = pc;

            // Fondo oscuro (el techo ya tapa el cielo) y linterna colgada de la camara: ilumina lo que mira el jugador
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.01f, 0.02f);
            // Sin linterna: la luz la ponen las lamparas de cada sala y sus interruptores
            var oldFlash = camGo.transform.Find("Flashlight");
            if (oldFlash != null) Object.DestroyImmediate(oldFlash.gameObject);

            // ---------- Flujo de partida ----------
            var flow = new GameObject("GameFlow");
            flow.AddComponent<ItemDatabase>().items = new[] { pistolItem, shotgunItem, handgunAmmo, shotgunAmmo, spray, key, keyExit };
            var gameFlow = flow.AddComponent<GameFlow>();
            gameFlow.startWeapon = pistolItem;
            gameFlow.startMagazine = 10;   // dos zombis normales a cuerpo (4 tiros) + 2 de margen

            // ---------- Audio ----------
            var ga = new GameObject("GameAudio").AddComponent<GameAudio>();
            ga.ambientLoop = Clip("music_ambient");
            ga.tensionLoop = Clip("music_tension");
            ga.musicVolume = 0.45f;
            ga.footsteps = Clips("step", 3);
            ga.zombieGroans = Clips("zgroan", 4);
            ga.zombieHurts = Clips("zhurt", 2);
            ga.zombieAttack = Clip("zattack");
            ga.zombieDeath = Clip("zdeath");
            ga.playerHurt = Clip("player_hurt");
            ga.playerDeath = Clip("player_death");
            ga.dryFire = Clip("dry_fire");
            ga.doorOpen = Clip("door_open");
            ga.doorClose = Clip("door_close");
            ga.doorLocked = Clip("door_locked");
            ga.doorUnlock = Clip("door_unlock");
            ga.pickup = Clip("pickup");
            ga.flashlight = Clip("flashlight");
            ga.lightSwitch = Clip("switch");
            ga.lampZap = Clip("lamp_zap");
            ga.lampHum = Clip("lamp_hum");
            ga.heartbeat = Clip("heartbeat");
            ga.stingers = new[] { Clip("sting_bang"), Clip("sting_creak"), Clip("sting_scrape") };
            pistol.heldScale = 1.3f;       // modelos nuevos de armas: escalas ajustadas a las manos del personaje
            shotgun.heldScale = 0.9f;
            pistol.fireSound = Clip("pistol_shot");
            pistol.reloadSound = Clip("reload");
            shotgun.fireSound = Clip("shotgun_shot");
            shotgun.reloadSound = Clip("reload");
            EditorUtility.SetDirty(pistol);
            EditorUtility.SetDirty(shotgun);

            var hud = new GameObject("HUD").AddComponent<Hud>();
            hud.playerHealth = player.GetComponent<Health>();
            hud.inventory = inv;
            hud.weapons = wc;
            hud.player = pc;

            // ---------- Zombis ----------
            var zombies = new GameObject("Zombies").transform;
            Zombie(new Vector3(-2.5f, 1, -8.5f), 0, zombies, 200f);   // civil, en el pasillo de la oficina
            Zombie(new Vector3(11.8f, 1, -17.2f), 1, zombies, 30f);   // policia, encerrado en el almacen (guarda la llave)
            Zombie(new Vector3(11.5f, 1, 1.5f), 2, zombies, 250f);    // paciente, oficina este
            Zombie(new Vector3(-12.5f, 1, -0.8f), 3, zombies, 60f);   // ejecutivo, despacho del capitan
            Zombie(new Vector3(5.2f, 1, -2.0f), 4, zombies, 160f);    // mecanico, junto a la barricada
            Zombie(new Vector3(6, 1, 10), 5, zombies, 180f);          // infectado, sala del fondo, tras la puerta

            // Se hornea con la puerta desactivada para que el hueco quede transitable. NavMeshModifier
            // (ignoreFromBuild) no se respeta aqui, asi que no se depende de el. En juego el
            // NavMeshObstacle de la puerta bloquea el paso solo mientras esta cerrada.
            foreach (var leaf in DoorLeaves) leaf.SetActive(false);
            surface.BuildNavMesh();
            foreach (var leaf in DoorLeaves) leaf.SetActive(true);

            // Los objetos del suelo se dejan caer y asentar aqui, para que la escena arranque con todo ya en reposo
            var oldMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            Physics.SyncTransforms();
            for (int i = 0; i < 200; i++) Physics.Simulate(0.02f);
            Physics.simulationMode = oldMode;

            // En juego se vuelve a hornear al arrancar (los datos horneados en el editor no se
            // persisten de forma fiable con la escena)
            var runtimeNav = root.AddComponent<RuntimeNavMesh>();
            runtimeNav.surface = surface;
            runtimeNav.disableDuringBake = DoorLeaves.ToArray();

            AssetDatabase.SaveAssets();
            Debug.Log("[Horror] Escena de prueba creada. Guarda la escena (Ctrl+S) y pulsa Play.");
        }

        const string AudioPath = "Assets/_Project/Audio/";

        static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath + name + ".wav");

        static AudioClip[] Clips(string prefix, int count)
        {
            var list = new System.Collections.Generic.List<AudioClip>();
            for (int i = 1; i <= count; i++)
            {
                var c = Clip(prefix + i);
                if (c != null) list.Add(c);
            }
            return list.ToArray();
        }

        /// <summary>Techo, luz ambiente, niebla, lamparas y vineta. Interior cerrado y oscuro, pero legible.</summary>
        static (CeilingLamp[] main, CeilingLamp[] back) BuildLighting(Transform root, Material ceilingMat)
        {
            // El techo convierte el nivel en un interior (sin cielo azul)
            Box("Ceiling", new Vector3(0, 3.5f, 0), new Vector3(30, 1, 40), ceilingMat, root, 2.4f);

            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type != LightType.Directional) continue;
                l.intensity = 0.3f;
                l.color = new Color(0.55f, 0.62f, 0.9f);
                l.shadows = LightShadows.None;   // relleno suave; el techo no debe oscurecerlo todo
            }

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.2f, 0.21f, 0.27f);   // sin linterna: con las luces apagadas aun se distingue algo
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.009f;   // poca: con mas, las salas grandes se ven negras al fondo
            RenderSettings.fogColor = new Color(0.04f, 0.05f, 0.07f);

            var warm = new Color(1f, 0.72f, 0.45f);
            var cold = new Color(0.8f, 0.92f, 1f);   // blanco frio (un azul saturado se ve mucho mas oscuro)
            // Comisaria (zona inicial). Cada lampara: foco con sombras hacia abajo + relleno sin sombras que ilumina
            // paredes y rincones de su sala (alcance ajustado a la sala para no atravesar tabiques).
            // Orden (lo usan los interruptores): 0 vestibulo, 1 vestuario, 2 almacen, 3..7 oficina, 8 despacho
            var main = new[]
            {
                Lamp(root, new Vector3(0, 0, -16.3f), warm, 64f, 13f, false, 5.8f, 26f),              // vestibulo
                Lamp(root, new Vector3(-10, 0, -16.3f), warm, 60f, 13f, false, 5.8f, 24f),            // vestuario
                Lamp(root, new Vector3(12, 0, -16.3f), cold, 56f, 13f, false, 5.8f, 22f),               // almacen
                Lamp(root, new Vector3(-10, 0, -7.5f), warm, 60f, 14f, false, 6.5f, 22f),               // oficina
                Lamp(root, new Vector3(0, 0, -7.5f), warm, 60f, 14f, false, 6.5f, 22f),
                Lamp(root, new Vector3(10, 0, -7.5f), warm, 60f, 14f, true, 6.5f, 22f),
                Lamp(root, new Vector3(-3, 0, 1f), warm, 56f, 14f, false, 8.5f, 22f),
                Lamp(root, new Vector3(9, 0, 1f), warm, 56f, 14f, false, 8.5f, 22f),
                Lamp(root, new Vector3(-11.5f, 0, 1), warm, 52f, 11f, false, 4.6f, 22f),              // despacho del capitan
            };
            // Sala del fondo: luz fria para distinguirla
            // Salas seguras: luz calida siempre encendida (sin interruptor)
            Lamp(root, new Vector3(7f, 0, -16.3f), warm, 40f, 9f, false, 4.5f, 18f);
            Lamp(root, new Vector3(-12.3f, 0, 17f), warm, 40f, 9f, false, 4.5f, 18f);
            var back = new[]
            {
                Lamp(root, new Vector3(-8, 0, 12.5f), cold, 56f, 14f, false, 8.5f, 22f),
                Lamp(root, new Vector3(0, 0, 12.5f), cold, 56f, 14f, false, 8.5f, 22f),
                Lamp(root, new Vector3(8, 0, 12.5f), cold, 56f, 14f, false, 8.5f, 22f),
            };

            var vol = Object.FindFirstObjectByType<UnityEngine.Rendering.Volume>();
            if (vol != null && vol.sharedProfile != null)
            {
                var profile = vol.sharedProfile;
                if (!profile.TryGet(out UnityEngine.Rendering.Universal.Vignette vignette))
                    vignette = profile.Add<UnityEngine.Rendering.Universal.Vignette>(true);
                vignette.intensity.Override(0.35f);
                vignette.smoothness.Override(0.5f);
                EditorUtility.SetDirty(profile);
            }
            return (main, back);
        }

        /// <summary>
        /// Resolucion de la sombra de una luz (nivel bajo/medio/alto del atlas). La propiedad publica solo se puede
        /// cambiar en juego, asi que se escribe en el campo serializado.
        /// </summary>
        static void SetShadowTier(Light light, int tier)
        {
            var data = light.GetUniversalAdditionalLightData();
            var so = new SerializedObject(data);
            var t = so.FindProperty("m_AdditionalLightsShadowResolutionTier");
            if (t != null) t.intValue = tier;
            var custom = so.FindProperty("m_UsePipelineSettings");
            if (custom != null) custom.boolValue = false;          // usar el nivel propio de la luz, no el global del pipeline
            so.ApplyModifiedProperties();
        }

        const float CeilingY = 3.0f;   // cara inferior del techo

        /// <summary>
        /// Plafon de techo: carcasa plana al ras del techo + panel emisivo, con un foco que apunta hacia abajo
        /// (no hacia el techo: asi no queda una mancha de luz sobre el propio plafon).
        /// </summary>
        static CeilingLamp Lamp(Transform parent, Vector3 xz, Color color, float intensity, float range, bool flicker,
                                float fillRange = 0f, float fillIntensity = 0f)
        {
            var go = new GameObject("Lamp");
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(xz.x, CeilingY - 0.12f, xz.z);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // mira hacia abajo

            var l = go.AddComponent<Light>();
            l.type = LightType.Spot;
            l.spotAngle = 125f;
            l.innerSpotAngle = 60f;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            // Sombras suaves: los muebles y las vigas proyectan su sombra en el suelo (gran parte del realismo)
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.95f;
            l.shadowBias = 0.04f;
            l.shadowNormalBias = 0.5f;
            SetShadowTier(l, UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium);

            // Relleno: luz puntual sin sombras a media altura (la lampara la enciende y apaga con el foco)
            if (fillRange > 0f && fillIntensity > 0f)
            {
                var fill = new GameObject("Fill");
                fill.transform.SetParent(go.transform, false);
                fill.transform.position = new Vector3(xz.x, 1.9f, xz.z);
                var fl = fill.AddComponent<Light>();
                fl.type = LightType.Point;
                fl.color = Color.Lerp(color, Color.white, 0.3f);
                fl.range = fillRange;
                fl.intensity = fillIntensity;
                fl.shadows = LightShadows.None;
                fill.AddComponent<FillLightRating>().rated = fillIntensity;
            }

            var lamp = go.AddComponent<CeilingLamp>();
            lamp.flicker = flicker;
            lamp.ratedIntensity = intensity;

            // Carcasa (pegada al techo) y panel emisivo (justo debajo). Se ponen en mundo y despues se emparentan.
            var housing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            housing.name = "Housing";
            housing.transform.position = new Vector3(xz.x, CeilingY - 0.03f, xz.z);
            housing.transform.localScale = new Vector3(1.3f, 0.07f, 0.42f);     // fluorescente de oficina
            Object.DestroyImmediate(housing.GetComponent<Collider>());
            housing.GetComponent<Renderer>().sharedMaterial = Mat("Housing", new Color(0.12f, 0.12f, 0.13f));
            housing.transform.SetParent(go.transform, true);

            var bulb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bulb.name = "Bulb";
            bulb.transform.position = new Vector3(xz.x, CeilingY - 0.065f, xz.z);
            bulb.transform.localScale = new Vector3(1.18f, 0.025f, 0.3f);
            Object.DestroyImmediate(bulb.GetComponent<Collider>());
            bulb.GetComponent<Renderer>().sharedMaterial = BulbMat(color);
            bulb.transform.SetParent(go.transform, true);
            return lamp;
        }

        static Material BulbMat(Color tint)
        {
            string path = $"{Mats}/Bulb_{ColorUtility.ToHtmlStringRGB(tint)}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool isNew = m == null;
            if (isNew) m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            // apagado se ve como un difusor gris; encendido brilla (la lampara escala la emision a 0 al apagarse)
            m.SetColor("_BaseColor", Color.Lerp(new Color(0.55f, 0.55f, 0.55f), tint, 0.25f));
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.Lerp(Color.white, tint, 0.6f) * 4f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            if (isNew) AssetDatabase.CreateAsset(m, path);
            // La palabra clave se pierde si solo se activa antes de crear el asset: sin ella la emision se ignora
            m.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssets();
            return m;
        }

        /// <summary>Interruptor de pared ("llave de luz"). Mira hacia -Z local; yaw 180 para el otro lado de la pared.</summary>
        static LightSwitch WallSwitch(Transform parent, Vector3 pos, float yaw, CeilingLamp[] lamps)
        {
            var go = new GameObject("LightSwitch");
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            var plate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plate.name = "Plate";
            plate.transform.SetParent(go.transform, false);
            plate.transform.localScale = new Vector3(0.12f, 0.18f, 0.03f);
            plate.GetComponent<Renderer>().sharedMaterial = Mat("SwitchPlate", new Color(0.55f, 0.55f, 0.5f));

            var pivot = new GameObject("LeverPivot").transform;
            pivot.SetParent(go.transform, false);
            pivot.localPosition = new Vector3(0f, 0f, -0.016f);

            var lever = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lever.name = "Lever";
            lever.transform.SetParent(pivot, false);
            lever.transform.localPosition = new Vector3(0f, 0f, -0.012f);
            lever.transform.localScale = new Vector3(0.05f, 0.1f, 0.026f);
            Object.DestroyImmediate(lever.GetComponent<Collider>());
            lever.GetComponent<Renderer>().sharedMaterial = Mat("SwitchLever", new Color(0.82f, 0.8f, 0.72f));

            var sw = go.AddComponent<LightSwitch>();
            sw.lamps = lamps;
            sw.lever = pivot;
            return sw;
        }

        /// <summary>
        /// Variante de zombi: modelo (FBX generado con IA), estilo de movimiento (animaciones propias, ver
        /// Tools/blender/build_characters.py ZOMBIE_STYLES) y estadisticas. Todas comparten esqueleto.
        /// La velocidad de persecucion coincide con la de la zancada de su estilo, para que no patinen.
        /// </summary>
        struct ZombieVariant
        {
            public string name, fbx, style;
            public Vector3 scale;
            public float health, chaseSpeed, damage;
        }

        static readonly ZombieVariant[] ZombieVariants =
        {
            new ZombieVariant { name = "Civil",     fbx = "ZombieGenerated",    style = "civil",    scale = Vector3.one, health = 100f, chaseSpeed = 1.4f, damage = 15f },
            new ZombieVariant { name = "Policia",   fbx = "ZombieGenCop",       style = "cop",      scale = Vector3.one, health = 130f, chaseSpeed = 1.4f, damage = 15f },
            new ZombieVariant { name = "Paciente",  fbx = "ZombieGenGown",      style = "gown",     scale = Vector3.one, health = 70f,  chaseSpeed = 1.8f, damage = 12f },
            new ZombieVariant { name = "Ejecutivo", fbx = "ZombieGenSuit",      style = "suit",     scale = Vector3.one, health = 100f, chaseSpeed = 1.3f, damage = 15f },
            new ZombieVariant { name = "Mecanico",  fbx = "ZombieGenOveralls",  style = "overalls", scale = new Vector3(1.05f, 1.0f, 1.05f), health = 180f, chaseSpeed = 1.0f, damage = 25f },
            new ZombieVariant { name = "Infectado", fbx = "ZombieGenBare",      style = "bare",     scale = Vector3.one, health = 90f,  chaseSpeed = 1.6f, damage = 15f },
        };

        static void Zombie(Vector3 pos, int variantIndex, Transform parent, float yaw = 0f)
        {
            var v = ZombieVariants[variantIndex % ZombieVariants.Length];
            var z = new GameObject("Zombie_" + v.name);
            z.transform.SetParent(parent);
            z.transform.position = pos;
            z.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var col = z.AddComponent<CapsuleCollider>();
            col.height = 2f; col.radius = 0.4f; col.center = Vector3.zero;
            string ctrl = AnimPath + "Zombie_" + v.style + ".overrideController";
            if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ctrl) == null) ctrl = AnimPath + "ZombieAnimator.controller";
            var model = AttachModel(z.transform, CharPath + v.fbx + ".fbx", ctrl);
            z.AddComponent<ZombieHitZones>();
            model.transform.localScale = v.scale;   // complexion: robusto / delgado / alto
            var agent = z.AddComponent<NavMeshAgent>();
            agent.height = 2f; agent.radius = 0.4f; agent.speed = v.chaseSpeed; agent.angularSpeed = 200f;
            agent.baseOffset = 1f;   // el pivote esta en el centro de la capsula (1 m sobre los pies)
            z.AddComponent<Health>().maxHealth = v.health;
            var ai = z.AddComponent<ZombieAI>();
            z.AddComponent<ZombieAudio>();
            ai.chaseSpeed = v.chaseSpeed;
            ai.attackDamage = v.damage;
            z.AddComponent<ZombieAnimation>().strideSpeed = v.chaseSpeed;
        }

        const string CharPath = "Assets/_Project/Art/Characters/";
        const string AnimPath = "Assets/_Project/Animation/";

        /// <summary>Instancia el FBX como hijo, con los pies alineados con la base del collider (altura 2).</summary>
        static GameObject AttachModel(Transform parent, string fbxPath, string controllerPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            model.name = "Model";
            model.transform.localPosition = new Vector3(0, -1f, 0);
            model.transform.localRotation = Quaternion.identity;
            var animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
            animator.applyRootMotion = false;
            return model;
        }

        /// <summary>
        /// Animador del jugador: en la capa base, al apuntar se pasa a la postura de tirador (de lado, AimIdle/AimWalk);
        /// y un override con las animaciones de arma larga (escopeta a dos manos). Idempotente.
        /// </summary>
        static void SetupPlayerAnimator()
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(AnimPath + "PlayerAnimator.controller");
            if (ctrl == null) return;
            var clips = new Dictionary<string, AnimationClip>();
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(CharPath + "Player.fbx"))
                if (o is AnimationClip c && !c.name.StartsWith("__")) clips[c.name] = c;
            if (!clips.ContainsKey("AimIdle") || !clips.ContainsKey("Aim_Long")) return;

            var baseSm = ctrl.layers[0].stateMachine;
            UnityEditor.Animations.AnimatorState loco = null, aimLoco = null;
            foreach (var st in baseSm.states)
            {
                if (st.state.name == "Locomotion") loco = st.state;
                if (st.state.name == "AimLocomotion") aimLoco = st.state;
            }
            if (loco != null && aimLoco == null)
            {
                aimLoco = baseSm.AddState("AimLocomotion", new Vector3(300, 120, 0));
                var tree = new UnityEditor.Animations.BlendTree { name = "AimLocomotion", blendParameter = "Speed", useAutomaticThresholds = false };
                AssetDatabase.AddObjectToAsset(tree, ctrl);
                tree.AddChild(clips["AimIdle"], 0f);
                tree.AddChild(clips["AimWalk"], 1.3f);
                aimLoco.motion = tree;
                var toAim = loco.AddTransition(aimLoco);
                toAim.hasExitTime = false; toAim.duration = 0.18f;
                toAim.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0f, "Aiming");
                var back = aimLoco.AddTransition(loco);
                back.hasExitTime = false; back.duration = 0.22f;
                back.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0f, "Aiming");
            }
            // transiciones del torso algo mas largas: cambios de pose fluidos
            foreach (var st in ctrl.layers[1].stateMachine.states)
                foreach (var t in st.state.transitions)
                    if (!t.hasExitTime && t.duration < 0.18f && st.state.name != "Aim") t.duration = 0.18f;
            EditorUtility.SetDirty(ctrl);

            // override de arma larga
            string ovPath = AnimPath + "PlayerAnimator_Long.overrideController";
            var ov = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ovPath);
            bool isNew = ov == null;
            if (isNew) ov = new AnimatorOverrideController(ctrl);
            else ov.runtimeAnimatorController = ctrl;
            var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            foreach (var orig in ctrl.animationClips)
            {
                clips.TryGetValue(orig.name + "_Long", out var rep);
                pairs.Add(new KeyValuePair<AnimationClip, AnimationClip>(orig, rep));
            }
            ov.ApplyOverrides(pairs);
            if (isNew) AssetDatabase.CreateAsset(ov, ovPath);
            EditorUtility.SetDirty(ov);
            AssetDatabase.SaveAssets();
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform c in root)
            {
                var r = FindDeep(c, name);
                if (r != null) return r;
            }
            return null;
        }

        /// <summary>
        /// Crea el soporte del arma bajo la mano derecha. Su rotacion se calcula con la pose
        /// real de "Aim", de modo que el cañon (+Z del modelo) apunte al frente al apuntar.
        /// </summary>
        static Transform CreateHandHolder(GameObject model, string aimClip, string holderName)
        {
            var hand = FindDeep(model.transform, "Hand.R");
            var holder = new GameObject(holderName).transform;
            holder.SetParent(hand, false);

            var lowerArm = hand.parent;

            // Guarda la pose actual, muestrea Aim, calcula la rotacion y restaura
            var all = model.GetComponentsInChildren<Transform>();
            var pos = new Vector3[all.Length];
            var rot = new Quaternion[all.Length];
            for (int i = 0; i < all.Length; i++) { pos[i] = all[i].localPosition; rot[i] = all[i].localRotation; }

            AnimationClip aim = null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(CharPath + "Player.fbx"))
                if (o is AnimationClip c && c.name == aimClip) aim = c;
            if (aim != null)
            {
                aim.SampleAnimation(model, 0f);
                holder.rotation = model.transform.rotation;   // cañon al frente, arma recta
                // El centro de la mano esta 7 cm mas alla del hueso, siguiendo la direccion del antebrazo
                Vector3 armDir = (hand.position - lowerArm.position).normalized;
                holder.position = hand.position + armDir * 0.07f;
                var local = holder.localRotation;
                var localPos = holder.localPosition;
                for (int i = 0; i < all.Length; i++) { all[i].localPosition = pos[i]; all[i].localRotation = rot[i]; }
                holder.localRotation = local;
                holder.localPosition = localPos;
            }
            return holder;
        }

        static void Pick(ItemData item, int count, Vector3 pos, Transform parent)
        {
            var p = Pickup.Spawn(item, count, pos);
            p.transform.SetParent(parent);
        }

        /// <summary>Friso de madera, zocalo, marcos de puerta, pilastras, vigas, tuberias y cartel de salida.</summary>
        static void BuildArchitecture(Transform level, Transform details, Material wall, Material wood, Material metal)
        {
            const float wain = 1.1f;   // altura del friso

            // Friso + moldura superior + zocalo en todas las caras interiores. (centro, largo, eje)
            // Eje 'x': pared que corre a lo largo de X (normal en Z); eje 'z': corre a lo largo de Z (normal en X).
            void Wainscot(string name, float fixedCoord, float center, float length, bool alongX, int inward)
            {
                // inward: +1/-1 = hacia donde mira la cara visible (sentido de la normal)
                float off = 0.02f * inward;
                Vector3 p(float along, float y, float depth) => alongX ? new Vector3(along, y, fixedCoord + depth * inward) : new Vector3(fixedCoord + depth * inward, y, along);
                Vector3 s(float len, float h, float depth) => alongX ? new Vector3(len, h, depth) : new Vector3(depth, h, len);
                Box(name + "_Wainscot", p(center, wain / 2f, 0.02f), s(length, wain, 0.04f), wood, details, 1f, false);
                Box(name + "_Rail", p(center, wain + 0.03f, 0.04f), s(length, 0.06f, 0.08f), wood, details, 1f, false);
                Box(name + "_Base", p(center, 0.07f, 0.04f), s(length, 0.14f, 0.08f), wood, details, 1f, false);
            }

            Wainscot("S", -19.5f, 0f, 29f, true, +1);          // pared sur (cara hacia +Z)
            Wainscot("N", 19.5f, 0f, 29f, true, -1);           // pared norte (cara hacia -Z)
            Wainscot("W", -14.5f, 0f, 39f, false, +1);         // oeste (cara hacia +X)
            Wainscot("E", 14.5f, 0f, 39f, false, -1);          // este (cara hacia -X)
            Wainscot("MidL_Main", 4.5f, -7.8f, 13.4f, true, -1);   // pared interior, lado sala principal
            Wainscot("MidR_Main", 4.5f, 7.8f, 13.4f, true, -1);
            Wainscot("MidL_Back", 5.5f, -7.8f, 13.4f, true, +1);   // pared interior, lado sala del fondo
            Wainscot("MidR_Back", 5.5f, 7.8f, 13.4f, true, +1);

            // Marco de la puerta, por las dos caras
            foreach (float z in new[] { 4.46f, 5.54f })
            {
                Box("DoorJamb_L", new Vector3(-1.07f, 1.25f, z), new Vector3(0.14f, 2.5f, 0.08f), wood, details, 1f, false);
                Box("DoorJamb_R", new Vector3(1.07f, 1.25f, z), new Vector3(0.14f, 2.5f, 0.08f), wood, details, 1f, false);
                Box("DoorHeader", new Vector3(0f, 2.5f, z), new Vector3(2.28f, 0.16f, 0.08f), wood, details, 1f, false);
            }

            // Pilastras (con colision: forman parte del nivel y del NavMesh)
            foreach (float z in new[] { -13f, -4f, 11f, 17f })
            {
                Box("Pilaster_W", new Vector3(-14.25f, 1.5f, z), new Vector3(0.5f, 3f, 0.5f), wall, level, 3f);
                Box("Pilaster_E", new Vector3(14.25f, 1.5f, z), new Vector3(0.5f, 3f, 0.5f), wall, level, 3f);
            }

            // Vigas del techo
            foreach (float z in new[] { -9f, -3.5f, 2f, 15f })
                Box("Beam", new Vector3(0f, 2.85f, z), new Vector3(29f, 0.3f, 0.4f), wood, details, 1f, false);

            // Tuberias a lo largo de las paredes, con abrazaderas
            void Pipe(string name, Vector3 center, float length, Quaternion rot, float radius)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name = name;
                go.transform.SetParent(details);
                go.transform.position = center;
                go.transform.rotation = rot;
                go.transform.localScale = new Vector3(radius * 2f, length / 2f, radius * 2f);
                go.GetComponent<Renderer>().sharedMaterial = metal;
                Object.DestroyImmediate(go.GetComponent<Collider>());
            }
            var alongZ = Quaternion.Euler(90f, 0f, 0f);
            Pipe("Pipe_W", new Vector3(-13.9f, 2.55f, -7.5f), 24f, alongZ, 0.07f);
            Pipe("Pipe_E", new Vector3(13.9f, 2.55f, -7.5f), 24f, alongZ, 0.07f);
            Pipe("Pipe_Cross", new Vector3(0f, 2.72f, -16.8f), 28.8f, Quaternion.Euler(0f, 0f, 90f), 0.06f);
            Pipe("Pipe_Back", new Vector3(13.9f, 2.55f, 12.5f), 13f, alongZ, 0.07f);
            for (float z = -18f; z <= 3f; z += 4f)
            {
                Box("Clamp", new Vector3(-13.9f, 2.55f, z), new Vector3(0.2f, 0.2f, 0.08f), metal, details, 1f, false);
                Box("Clamp", new Vector3(13.9f, 2.55f, z), new Vector3(0.2f, 0.2f, 0.08f), metal, details, 1f, false);
            }

            // Cartel de salida sobre la puerta (verde, emisivo) con un reflejo verdoso
            var sign = Box("ExitSign", new Vector3(0f, 2.7f, 4.47f), new Vector3(0.6f, 0.2f, 0.05f), SignMat(), details, 0f, false);
            var glow = new GameObject("ExitSignGlow");
            glow.transform.SetParent(details);
            glow.transform.position = new Vector3(0f, 2.55f, 4.1f);
            var gl = glow.AddComponent<Light>();
            gl.type = LightType.Point; gl.color = new Color(0.2f, 1f, 0.45f); gl.intensity = 1.2f; gl.range = 3.5f; gl.shadows = LightShadows.None;
        }

        /// <summary>Sondas de reflejo (el suelo brillante refleja las lamparas y el techo) y polvo en suspension.</summary>
        static void BuildAtmosphere(Transform details)
        {
            void Probe(string name, Vector3 center, Vector3 size)
            {
                var go = new GameObject(name);
                go.transform.SetParent(details);
                go.transform.position = center;
                var rp = go.AddComponent<ReflectionProbe>();
                rp.mode = ReflectionProbeMode.Realtime;
                rp.refreshMode = ReflectionProbeRefreshMode.OnAwake;     // se captura una vez al empezar
                rp.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
                rp.resolution = 256;
                rp.hdr = true;
                rp.boxProjection = true;                                 // el reflejo se deforma segun el tamano de la sala
                rp.size = size;
                rp.intensity = 1f;
                rp.blendDistance = 1f;
                rp.clearFlags = ReflectionProbeClearFlags.SolidColor;
                rp.backgroundColor = new Color(0.02f, 0.02f, 0.03f);
                rp.farClipPlane = 60f;
            }
            Probe("ReflectionProbe_Main", new Vector3(0f, 1.5f, -7.5f), new Vector3(29f, 3f, 24f));
            Probe("ReflectionProbe_Back", new Vector3(0f, 1.5f, 12.5f), new Vector3(29f, 3f, 14f));

            var mat = DustMat();
            void Dust(string name, Vector3 center, Vector3 size, int count)
            {
                var go = new GameObject(name);
                go.transform.SetParent(details);
                go.transform.position = center;
                var ps = go.AddComponent<ParticleSystem>();
                var main = ps.main;
                main.loop = true;
                main.prewarm = true;
                main.startLifetime = 16f;
                main.startSpeed = 0f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.035f);
                main.startColor = new Color(1f, 0.95f, 0.85f, 0.45f);
                main.maxParticles = count;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                var em = ps.emission;
                em.rateOverTime = count / 16f;
                var sh = ps.shape;
                sh.shapeType = ParticleSystemShapeType.Box;
                sh.scale = size;
                var noise = ps.noise;
                noise.enabled = true;
                noise.strength = 0.12f;
                noise.frequency = 0.25f;
                noise.scrollSpeed = 0.1f;
                noise.quality = ParticleSystemNoiseQuality.Medium;
                var fade = ps.colorOverLifetime;
                fade.enabled = true;
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                          new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) });
                fade.color = g;
                var pr = go.GetComponent<ParticleSystemRenderer>();
                pr.sharedMaterial = mat;
                pr.shadowCastingMode = ShadowCastingMode.Off;
                pr.receiveShadows = false;
            }
            Dust("Dust_Main", new Vector3(0f, 1.5f, -7.5f), new Vector3(28f, 2.8f, 23f), 1400);
            Dust("Dust_Back", new Vector3(0f, 1.5f, 12.5f), new Vector3(28f, 2.8f, 13f), 800);
        }

        /// <summary>Material de las motas de polvo: particula "Lit" transparente (solo se ven donde llega la luz).</summary>
        static Material DustMat()
        {
            string texPath = "Assets/_Project/Art/Textures/particle_soft.png";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(texPath) == null)
            {
                const int n = 64;
                var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f - 0.5f, n / 2f - 0.5f)) / (n / 2f);
                        float a = Mathf.Clamp01(1f - d);
                        a = a * a * (3f - 2f * a);
                        t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                    }
                File.WriteAllBytes(texPath, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(texPath);
                var ti = (TextureImporter)AssetImporter.GetAtPath(texPath);
                ti.alphaIsTransparency = true;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.mipmapEnabled = true;
                ti.SaveAndReimport();
            }

            string path = $"{Mats}/Dust.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Particles/Lit"));
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
            m.SetColor("_BaseColor", Color.white);
            m.SetFloat("_Surface", 1f);                         // transparente
            m.SetFloat("_Blend", 0f);                           // mezcla alfa
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Smoothness", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(m, path);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");       // las palabras clave se pierden si solo se activan antes de crear el asset
            m.DisableKeyword("_ALPHATEST_ON");
            EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssets();
            return m;
        }

        /// <summary>Puerta de salida (pared norte, sala del fondo) con su cartel, y los terminales de guardado.</summary>
        static void BuildExitAndTerminals(Transform level, ItemData keyExit, Material metal)
        {
            // Puerta de salida: hoja metalica con marco, justo contra la pared norte
            var exit = Box("ExitDoor", new Vector3(0f, 1.15f, 19.40f), new Vector3(1.9f, 2.3f, 0.14f), metal, level, 1.5f);
            exit.AddComponent<ExitDoor>().requiredKey = keyExit;
            Box("ExitFrame_L", new Vector3(-1.02f, 1.2f, 19.40f), new Vector3(0.14f, 2.4f, 0.18f), metal, level, 1f, false);
            Box("ExitFrame_R", new Vector3(1.02f, 1.2f, 19.40f), new Vector3(0.14f, 2.4f, 0.18f), metal, level, 1f, false);
            Box("ExitFrame_T", new Vector3(0f, 2.37f, 19.40f), new Vector3(2.18f, 0.14f, 0.18f), metal, level, 1f, false);
            Box("ExitSign", new Vector3(0f, 2.72f, 19.44f), new Vector3(0.6f, 0.2f, 0.05f), SignMat(), level, 0f, false);
            var glow = new GameObject("ExitGlow");
            glow.transform.SetParent(level);
            glow.transform.position = new Vector3(0f, 2.5f, 19.0f);
            var gl = glow.AddComponent<Light>();
            gl.type = LightType.Point; gl.color = new Color(0.2f, 1f, 0.45f); gl.intensity = 1.2f; gl.range = 3.5f; gl.shadows = LightShadows.None;

            // Terminales de guardado: junto al punto de aparicion y en la sala del fondo
            SaveTerminalProp(level, new Vector3(7.9f, 0f, -19.1f), +1, metal);     // sala segura 1 (junto al vestibulo)
            SaveTerminalProp(level, new Vector3(-11.4f, 0f, 19.1f), -1, metal);    // sala segura 2 (sala del fondo)
        }

        /// <summary>facing: +1 si la sala esta hacia +Z (pared sur), -1 si esta hacia -Z (pared norte).</summary>
        static void SaveTerminalProp(Transform level, Vector3 pos, int facing, Material metal)
        {
            var ped = Box("SaveTerminal", pos + new Vector3(0f, 0.55f, 0f), new Vector3(0.5f, 1.1f, 0.4f), metal, level, 1f);
            ped.AddComponent<SaveTerminal>();
            var screen = Box("Screen", pos + new Vector3(0f, 1.16f, 0.04f * facing), new Vector3(0.44f, 0.04f, 0.30f), TerminalMat(), level, 0f, false);
            screen.transform.rotation = Quaternion.Euler(-30f * facing, 0f, 0f);
            Box("ScreenBase", pos + new Vector3(0f, 1.11f, 0f), new Vector3(0.50f, 0.04f, 0.40f), metal, level, 1f, false);
            var l = new GameObject("TerminalGlow");
            l.transform.SetParent(level);
            l.transform.position = pos + new Vector3(0f, 1.5f, 0.35f * facing);
            var li = l.AddComponent<Light>();
            li.type = LightType.Point; li.color = new Color(0.4f, 0.9f, 1f); li.intensity = 0.9f; li.range = 2.8f; li.shadows = LightShadows.None;
        }

        static Material TerminalMat()
        {
            string path = $"{Mats}/Terminal.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", new Color(0.1f, 0.4f, 0.5f));
            m.SetColor("_EmissionColor", new Color(0.3f, 0.9f, 1f) * 1.8f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            AssetDatabase.CreateAsset(m, path);
            m.EnableKeyword("_EMISSION");        // se pierde si solo se activa antes de crear el asset
            EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssets();
            return m;
        }

        static Material SignMat()
        {
            string path = $"{Mats}/ExitSign.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", new Color(0.1f, 0.6f, 0.25f));
            m.SetColor("_EmissionColor", new Color(0.15f, 1f, 0.4f) * 2f);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            AssetDatabase.CreateAsset(m, path);
            m.EnableKeyword("_EMISSION");        // la palabra clave se pierde si solo se activa antes de crear el asset
            EditorUtility.SetDirty(m);
            AssetDatabase.SaveAssets();
            return m;
        }

        /// <summary>Instancia un modelo de atrezzo con yaw y le ajusta un BoxCollider (origen = base).</summary>
        static GameObject Prop(string name, Vector3 pos, float yaw, Transform parent, float scale = 1f)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PropsPath + name + ".fbx");
            if (prefab == null) return null;
            var holder = new GameObject(name);
            holder.transform.SetParent(parent);
            holder.transform.position = pos;
            holder.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            holder.transform.localScale = Vector3.one * scale;
            var model = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder.transform);
            model.transform.localPosition = Vector3.zero;     // se conserva la rotacion del prefab (eje del FBX)
            Pickup.FitBoxCollider(holder);
            return holder;
        }

        /// <summary>
        /// Zona inicial: planta de comisaria. El antiguo salon diafano (30 x 25 m) se divide en vestibulo (aparicion),
        /// vestuario, almacen, oficina con puestos de trabajo y despacho del capitan. Los tabiques tienen colision,
        /// asi que el NavMesh los respeta y los zombis rodean por las puertas.
        /// </summary>
        static void BuildPoliceStation(Transform level, Transform details, Material wall, Material wood, Material metal)
        {
            var st = new GameObject("PoliceStation").transform;
            st.SetParent(level);
            // Tabiques: (x0,z0)-(x1,z1) sobre el suelo, alineados a un eje
            Partition(st, details, wall, wood, "Lobby_N_W", new Vector2(-14.5f, -13f), new Vector2(-0.75f, -13f));
            Partition(st, details, wall, wood, "Lobby_N_C", new Vector2(0.75f, -13f), new Vector2(9.5f, -13f));
            Partition(st, details, wall, wood, "Lobby_N_E", new Vector2(10.9f, -13f), new Vector2(14.5f, -13f));
            Lintel(st, wall, new Vector3(0f, 0f, -13f), 1.5f, true);
            Lintel(st, wall, new Vector3(10.2f, 0f, -13f), 1.4f, true);
            Partition(st, details, wall, wood, "Lobby_W_S", new Vector2(-5f, -19.5f), new Vector2(-5f, -17.6f));
            Partition(st, details, wall, wood, "Lobby_W_N", new Vector2(-5f, -16.2f), new Vector2(-5f, -13.1f));
            Lintel(st, wall, new Vector3(-5f, 0f, -16.9f), 1.4f, false);
            // Sala segura 1 (x 5..9): puerta desde el vestibulo; el almacen queda de x 9 a 15
            Partition(st, details, wall, wood, "Lobby_E_S", new Vector2(5f, -19.5f), new Vector2(5f, -17.6f));
            Partition(st, details, wall, wood, "Lobby_E_N", new Vector2(5f, -16.2f), new Vector2(5f, -13.1f));
            Lintel(st, wall, new Vector3(5f, 0f, -16.9f), 1.4f, false);
            Partition(st, details, wall, wood, "Safe1_E", new Vector2(9f, -19.5f), new Vector2(9f, -13.1f));
            MakeDoor(st, new Vector3(5f, 0f, -17.6f), -90f, 1.4f, wood, metal, false, "Door_SafeRoom1", 0.2f).zombiesCanForce = false;
            // Sala segura 2 (esquina noroeste de la sala del fondo, x -14.5..-10, z 14.5..19.5)
            Partition(st, details, wall, wood, "Safe2_S", new Vector2(-14.5f, 14.5f), new Vector2(-10f, 14.5f));
            Partition(st, details, wall, wood, "Safe2_E_S", new Vector2(-10f, 14.6f), new Vector2(-10f, 15.0f));
            Partition(st, details, wall, wood, "Safe2_E_N", new Vector2(-10f, 16.4f), new Vector2(-10f, 19.5f));
            Lintel(st, wall, new Vector3(-10f, 0f, 15.7f), 1.4f, false);
            MakeDoor(st, new Vector3(-10f, 0f, 15.0f), -90f, 1.4f, wood, metal, false, "Door_SafeRoom2", 0.2f).zombiesCanForce = false;
            // Despacho del capitan (esquina noroeste de la oficina)
            Partition(st, details, wall, wood, "Captain_S", new Vector2(-14.5f, -3f), new Vector2(-8f, -3f));
            Partition(st, details, wall, wood, "Captain_E_S", new Vector2(-8f, -2.9f), new Vector2(-8f, 0.4f));
            Partition(st, details, wall, wood, "Captain_E_N", new Vector2(-8f, 1.8f), new Vector2(-8f, 4.5f));
            Lintel(st, wall, new Vector3(-8f, 0f, 1.1f), 1.4f, false);

            // Puertas de madera con ventanuco: cortan el paso a los zombis, que acaban forzandolas tras unos segundos
            MakeDoor(st, new Vector3(-0.75f, 0f, -13f), 0f, 1.5f, wood, metal, false, "Door_Lobby", 0.2f);
            MakeDoor(st, new Vector3(9.5f, 0f, -13f), 0f, 1.4f, wood, metal, false, "Door_Storage", 0.2f);
            MakeDoor(st, new Vector3(-5f, 0f, -17.6f), -90f, 1.4f, wood, metal, false, "Door_Lockers", 0.2f);
            MakeDoor(st, new Vector3(-8f, 0f, 0.4f), -90f, 1.4f, wood, metal, false, "Door_Captain", 0.2f);

            var props = new GameObject("Props").transform;
            props.SetParent(level);
            // Los muebles no son suelo: sin esto el NavMesh cubre mesas y sillas y los zombis se suben por encima
            var notWalkable = props.gameObject.AddComponent<NavMeshModifier>();
            notWalkable.overrideArea = true;
            notWalkable.area = 1;   // Not Walkable
            notWalkable.applyToChildren = true;
            var partitionMat = Mat("CubiclePanel", new Color(0.32f, 0.34f, 0.38f));

            // ---- Vestibulo: mostrador de recepcion a los lados del paso a la oficina, bancos y archivo
            Prop("Desk", new Vector3(-3.0f, 0f, -13.62f), 180f, props);
            Prop("Desk", new Vector3(3.0f, 0f, -13.62f), 180f, props);
            Prop("Chair", new Vector3(-3.2f, 0f, -14.6f), 10f, props);
            Prop("FilingCabinet", new Vector3(-4.55f, 0f, -18.9f), 90f, props);
            Prop("Chair", new Vector3(-2.0f, 0f, -19.1f), 0f, props);
            Prop("Chair", new Vector3(-1.4f, 0f, -19.1f), -6f, props);

            // ---- Vestuario: fila de taquillas contra la pared sur (dos abiertas) y otra en la norte, banco central
            float[] lx = { -13.95f, -13.05f, -12.15f, -11.25f, -10.35f, -9.45f, -8.55f, -7.65f };
            // todas se pueden abrir; dentro de algunas hay objetos (ver Pick en Build). Una ya abierta, de reclamo
            for (int i = 0; i < lx.Length; i++)
                Locker(props, metal, new Vector3(lx[i], 0f, -19.25f), 0f, i == 4);
            foreach (float x in new[] { -13.5f, -12.6f, -11.7f, -7.4f, -6.5f })
                Prop("Locker", new Vector3(x, 0f, -13.35f), 180f, props);
            Box("Bench_Seat", new Vector3(-10.4f, 0.42f, -16.3f), new Vector3(3.2f, 0.06f, 0.42f), wood, props);
            foreach (float x in new[] { -11.8f, -9.0f })
                Box("Bench_Leg", new Vector3(x, 0.2f, -16.3f), new Vector3(0.08f, 0.4f, 0.36f), metal, props);

            // ---- Almacen de pruebas: estanterias metalicas, cajas y bidones
            MetalShelf(props, metal, new Vector3(10.4f, 0f, -19.2f), 0f);
            MetalShelf(props, metal, new Vector3(11.7f, 0f, -19.2f), 0f);
            MetalShelf(props, metal, new Vector3(11.4f, 0f, -16.2f), 0f);
            MetalShelf(props, metal, new Vector3(12.7f, 0f, -16.2f), 0f);
            MetalShelf(props, metal, new Vector3(14.2f, 0f, -16.3f), 90f);
            Prop("Crate", new Vector3(13.8f, 0f, -18.9f), 0f, props);
            Prop("Crate", new Vector3(13.8f, 0.92f, -18.9f), 30f, props);

            // ---- Salas seguras: baul de objetos (comun a todas), una silla y un banco
            ItemBoxProp(props, wood, metal, new Vector3(6.0f, 0f, -19.1f), 0f);
            Prop("Chair", new Vector3(8.3f, 0f, -18.3f), 200f, props);
            Box("Safe1_Bench", new Vector3(5.45f, 0.42f, -14.6f), new Vector3(0.42f, 0.06f, 2.2f), wood, props);
            ItemBoxProp(props, wood, metal, new Vector3(-14.1f, 0f, 15.4f), 90f);
            Prop("Barrel", new Vector3(13.8f, 0f, -13.7f), 0f, props);
            Prop("Barrel", new Vector3(13.0f, 0f, -14.2f), 40f, props);

            // ---- Oficina: puestos de trabajo en dos filas a cada lado del pasillo central, con mamparas
            foreach (float z in new[] { -10.5f, -6.5f })
                foreach (float x in new[] { -11.2f, -5.6f, 5.6f, 11.2f })
                {
                    Prop("Desk", new Vector3(x, 0f, z), 180f, props);
                    Prop("Chair", new Vector3(x + Random.Range(-0.3f, 0.3f), 0f, z - 0.95f), Random.Range(-35f, 35f), props);
                    // mampara detras (norte) y lateral de cada puesto
                    Box("Cubicle_Back", new Vector3(x, 0.62f, z + 0.5f), new Vector3(1.8f, 1.24f, 0.06f), partitionMat, props);
                    Box("Cubicle_Side", new Vector3(x + (x < 0 ? 0.92f : -0.92f), 0.62f, z - 0.2f), new Vector3(0.06f, 1.24f, 1.4f), partitionMat, props);
                }
            // isla central: dos mesas enfrentadas con mampara en medio (parte el pasillo en dos de ~2.7 m)
            Prop("Desk", new Vector3(0f, 0f, -9.0f), 0f, props);
            Prop("Desk", new Vector3(0f, 0f, -7.9f), 180f, props);
            Box("Island_Panel", new Vector3(0f, 0.62f, -8.45f), new Vector3(1.8f, 1.24f, 0.06f), partitionMat, props);
            Prop("Chair", new Vector3(0.2f, 0f, -9.95f), 170f, props);
            Prop("Chair", new Vector3(-0.3f, 0f, -6.95f), 20f, props);
            // columnas estructurales del suelo al techo
            foreach (var c in new[] { new Vector2(-3.4f, -3.2f), new Vector2(3.4f, -3.2f), new Vector2(-3.4f, 2.2f), new Vector2(3.4f, 2.2f) })
                Box("Column", new Vector3(c.x, 1.5f, c.y), new Vector3(0.5f, 3f, 0.5f), wall, st, 3f);
            Prop("Locker", new Vector3(-14.2f, 0f, -9.0f), 90f, props);
            Prop("Locker", new Vector3(-14.2f, 0f, -8.1f), 90f, props);
            Prop("FilingCabinet", new Vector3(14.2f, 0f, -8.0f), -90f, props);
            Prop("FilingCabinet", new Vector3(14.2f, 0f, -7.3f), -90f, props);
            Prop("FilingCabinet", new Vector3(14.2f, 0f, -2.0f), -90f, props);
            Prop("FilingCabinet", new Vector3(14.2f, 0f, -1.3f), -90f, props);
            Prop("FilingCabinet", new Vector3(14.2f, 0f, -0.6f), -90f, props);
            Prop("Shelf", new Vector3(9f, 0f, 4.27f), 180f, props);
            Prop("Shelf", new Vector3(6.5f, 0f, 4.27f), 180f, props);
            // barricada improvisada en la oficina este: cajas y una silla tirada
            Prop("Crate", new Vector3(8.6f, 0f, -2.0f), 25f, props);
            Prop("Crate", new Vector3(9.7f, 0f, -2.4f), 5f, props);
            Prop("Crate", new Vector3(9.1f, 0.92f, -2.2f), 40f, props);
            Prop("Rubble", new Vector3(-3.4f, 0f, -11f), 30f, props);
            Prop("Rubble", new Vector3(2.5f, 0f, 2.2f), 110f, props);

            // ---- Despacho del capitan
            Prop("Desk", new Vector3(-11.5f, 0f, 1.0f), 90f, props);
            Prop("Chair", new Vector3(-12.6f, 0f, 1.1f), 100f, props);
            Prop("Shelf", new Vector3(-11.3f, 0f, 4.27f), 180f, props);
            Prop("FilingCabinet", new Vector3(-14.2f, 0f, -2.4f), 90f, props);

            // ---- Sala del fondo
            Prop("Cot", new Vector3(-13.5f, 0f, 17f), 0f, props);
            Prop("Desk", new Vector3(9f, 0f, 19.1f), 180f, props);
            Prop("Chair", new Vector3(9f, 0f, 17.9f), 170f, props);
            Prop("Desk", new Vector3(-7.8f, 0f, 19.1f), 180f, props);     // mesa de la armeria
            Prop("Shelf", new Vector3(-6f, 0f, 19.25f), 180f, props);
            Prop("FilingCabinet", new Vector3(6.5f, 0f, 19.2f), 180f, props);
            Locker(props, metal, new Vector3(14.25f, 0f, 12.0f), -90f, false);
            Prop("Locker", new Vector3(14.2f, 0f, 12.9f), -90f, props);
            Prop("Crate", new Vector3(13.3f, 0f, 18.3f), 15f, props);
            Prop("Crate", new Vector3(-13.3f, 0f, 9.0f), -12f, props);
            Prop("Barrel", new Vector3(-13.9f, 0f, 10.4f), 0f, props);
        }

        /// <summary>Tabique de 3 m (hasta el techo) entre dos puntos alineados a un eje, con zocalo por ambas caras.</summary>
        static void Partition(Transform parent, Transform details, Material wall, Material wood, string name, Vector2 a, Vector2 b)
        {
            const float t = 0.2f;
            bool alongX = Mathf.Abs(b.x - a.x) > Mathf.Abs(b.y - a.y);
            float len = alongX ? Mathf.Abs(b.x - a.x) : Mathf.Abs(b.y - a.y);
            var c = new Vector3((a.x + b.x) / 2f, 1.5f, (a.y + b.y) / 2f);
            var size = alongX ? new Vector3(len, 3f, t) : new Vector3(t, 3f, len);
            Box(name, c, size, wall, parent, 3f);
            foreach (int side in new[] { -1, 1 })
            {
                var off = alongX ? new Vector3(0f, 0f, side * (t / 2f + 0.02f)) : new Vector3(side * (t / 2f + 0.02f), 0f, 0f);
                var bsize = alongX ? new Vector3(len, 0.14f, 0.04f) : new Vector3(0.04f, 0.14f, len);
                Box(name + "_Base", new Vector3(c.x, 0.07f, c.z) + off, bsize, wood, details, 1f, false);
            }
        }

        /// <summary>Dintel sobre un hueco de paso (de 2.4 m al techo).</summary>
        static void Lintel(Transform parent, Material wall, Vector3 center, float width, bool alongX)
        {
            Box("Lintel", new Vector3(center.x, 2.7f, center.z), alongX ? new Vector3(width, 0.6f, 0.2f) : new Vector3(0.2f, 0.6f, width), wall, parent, 3f);
        }

        /// <summary>
        /// Taquilla construida con piezas (con colision) que se abre con E (LockerDoor). Se puede dejar un objeto en
        /// su balda (y = 1.32) o en el fondo (y = 0.1); no se puede coger hasta abrirla. Frente hacia +Z local.
        /// </summary>
        static GameObject Locker(Transform parent, Material metal, Vector3 pos, float yaw, bool startOpen)
        {
            var root = new GameObject("Locker_Openable");
            root.transform.SetParent(parent);
            var paint = Mat("LockerPaint", new Color(0.28f, 0.33f, 0.36f));
            void Part(string n, Vector3 p, Vector3 s, Transform par) => Box(n, p, s, paint, par);
            Part("Back", new Vector3(0f, 0.925f, -0.235f), new Vector3(0.9f, 1.85f, 0.03f), root.transform);
            Part("Side_L", new Vector3(-0.435f, 0.925f, 0f), new Vector3(0.03f, 1.85f, 0.5f), root.transform);
            Part("Side_R", new Vector3(0.435f, 0.925f, 0f), new Vector3(0.03f, 1.85f, 0.5f), root.transform);
            Part("Top", new Vector3(0f, 1.835f, 0f), new Vector3(0.9f, 0.03f, 0.5f), root.transform);
            Part("Base", new Vector3(0f, 0.05f, 0f), new Vector3(0.9f, 0.1f, 0.5f), root.transform);
            Part("Shelf", new Vector3(0f, 1.3f, 0.01f), new Vector3(0.84f, 0.025f, 0.46f), root.transform);
            Box("Hook", new Vector3(0f, 1.6f, -0.2f), new Vector3(0.04f, 0.04f, 0.06f), metal, root.transform, 0f, false);
            var hinge = new GameObject("DoorHinge").transform;
            hinge.SetParent(root.transform, false);
            hinge.localPosition = new Vector3(-0.45f, 0f, 0.25f);
            LocalBox("Door", new Vector3(0.44f, 0.925f, 0.013f), new Vector3(0.88f, 1.8f, 0.025f), paint, hinge, true);
            LocalBox("Vents", new Vector3(0.44f, 1.6f, 0.028f), new Vector3(0.5f, 0.12f, 0.005f), metal, hinge, false);
            LocalBox("Handle", new Vector3(0.8f, 1.0f, 0.04f), new Vector3(0.03f, 0.14f, 0.03f), metal, hinge, false);
            var ld = root.AddComponent<LockerDoor>();
            ld.hinge = hinge;
            ld.openAngle = -110f;
            if (startOpen) ld.SetOpen(true);
            root.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            return root;
        }

        /// <summary>Baul de objetos de madera con herrajes; la tapa (bisagra trasera) se levanta al abrirlo.</summary>
        static GameObject ItemBoxProp(Transform parent, Material wood, Material metal, Vector3 pos, float yaw)
        {
            var root = new GameObject("ItemBox");
            root.transform.SetParent(parent);
            var trunk = Env("Env_Wood", wood);
            LocalBox("Body", new Vector3(0f, 0.27f, 0f), new Vector3(1.0f, 0.54f, 0.55f), trunk, root.transform, true);
            foreach (float x in new[] { -0.38f, 0.38f })
                LocalBox("Band", new Vector3(x, 0.27f, 0f), new Vector3(0.05f, 0.56f, 0.57f), metal, root.transform, false);
            LocalBox("Lock", new Vector3(0f, 0.47f, 0.285f), new Vector3(0.1f, 0.1f, 0.02f), metal, root.transform, false);
            var hinge = new GameObject("LidHinge").transform;
            hinge.SetParent(root.transform, false);
            hinge.localPosition = new Vector3(0f, 0.54f, -0.275f);
            LocalBox("Lid", new Vector3(0f, 0.045f, 0.285f), new Vector3(1.02f, 0.09f, 0.58f), trunk, hinge, false);
            foreach (float x in new[] { -0.38f, 0.38f })
                LocalBox("LidBand", new Vector3(x, 0.05f, 0.285f), new Vector3(0.05f, 0.1f, 0.6f), metal, hinge, false);
            var box = root.AddComponent<ItemBox>();
            box.lid = hinge;
            root.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            return root;
        }

        /// <summary>Caja con posicion y rotacion LOCALES respecto a 'parent'.</summary>
        static GameObject LocalBox(string name, Vector3 localPos, Vector3 size, Material mat, Transform parent, bool collider)
        {
            var go = Box(name, Vector3.zero, size, mat, parent, 0f, collider);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            return go;
        }

        /// <summary>Hojas de todas las puertas: se desactivan al hornear el NavMesh para que el hueco quede transitable.</summary>
        static readonly List<GameObject> DoorLeaves = new List<GameObject>();

        /// <summary>
        /// Puerta abatible completa en un hueco: bisagra en 'hingePos', hoja hacia +X local (yaw 0 = pared a lo largo
        /// de X; yaw -90 = pared a lo largo de Z). security: hoja metalica con barra; si no, madera con ventanuco.
        /// frameDepth > 0 anade el marco (grosor de la pared). Cerrada bloquea el NavMesh (NavMeshObstacle).
        /// </summary>
        static Door MakeDoor(Transform parent, Vector3 hingePos, float yaw, float width, Material leafMat, Material metal, bool security, string name, float frameDepth = 0f)
        {
            var frame = new GameObject(name);
            frame.transform.SetParent(parent);
            frame.transform.SetPositionAndRotation(hingePos, Quaternion.Euler(0f, yaw, 0f));
            var hinge = new GameObject("Hinge").transform;
            hinge.SetParent(frame.transform, false);

            float w = width - 0.04f, h = 2.36f, t = security ? 0.07f : 0.05f;
            float cx = 0.02f + w / 2f;
            var slab = LocalBox("Leaf", new Vector3(cx, 0.02f + h / 2f, 0f), new Vector3(w, h, t), leafMat, hinge, true);
            var glass = Mat("DoorGlass", new Color(0.05f, 0.07f, 0.08f));
            glass.SetFloat("_Smoothness", 0.92f);
            foreach (int side in new[] { -1, 1 })
            {
                float z = side * (t / 2f + 0.006f);
                if (security)
                {
                    LocalBox("Window", new Vector3(cx, 1.7f, z), new Vector3(w * 0.25f, 0.35f, 0.012f), glass, hinge, false);
                    LocalBox("PushBar", new Vector3(cx, 1.0f, side * (t / 2f + 0.05f)), new Vector3(w * 0.75f, 0.05f, 0.05f), metal, hinge, false);
                    LocalBox("KickPlate", new Vector3(cx, 0.17f, z), new Vector3(w * 0.96f, 0.3f, 0.01f), metal, hinge, false);
                }
                else
                {
                    LocalBox("Window", new Vector3(cx, 1.72f, z), new Vector3(w * 0.42f, 0.5f, 0.012f), glass, hinge, false);
                    LocalBox("PanelLow", new Vector3(cx, 0.6f, z), new Vector3(w * 0.7f, 0.7f, 0.008f), leafMat, hinge, false);
                    LocalBox("Handle", new Vector3(w - 0.1f, 1.0f, side * (t / 2f + 0.035f)), new Vector3(0.14f, 0.025f, 0.025f), metal, hinge, false);
                    LocalBox("Escutcheon", new Vector3(w - 0.05f, 1.0f, side * (t / 2f + 0.005f)), new Vector3(0.05f, 0.16f, 0.01f), metal, hinge, false);
                    LocalBox("KickPlate", new Vector3(cx, 0.12f, z), new Vector3(w * 0.96f, 0.2f, 0.008f), metal, hinge, false);
                }
            }
            if (frameDepth > 0f)
            {
                var trim = Env("Env_Wood", Mat("Door", new Color(0.35f, 0.2f, 0.1f)));
                foreach (int side in new[] { -1, 1 })
                {
                    float z = side * (frameDepth / 2f + 0.025f);
                    LocalBox("Jamb_A", new Vector3(-0.04f, 1.2f, z), new Vector3(0.1f, 2.45f, 0.05f), trim, frame.transform, false);
                    LocalBox("Jamb_B", new Vector3(width + 0.04f, 1.2f, z), new Vector3(0.1f, 2.45f, 0.05f), trim, frame.transform, false);
                    LocalBox("Header", new Vector3(width / 2f, 2.45f, z), new Vector3(width + 0.18f, 0.1f, 0.05f), trim, frame.transform, false);
                }
            }
            // La hoja no entra en el horneado del NavMesh; el obstaculo con carving bloquea el paso solo si esta cerrada
            slab.AddComponent<NavMeshModifier>().ignoreFromBuild = true;
            var obs = slab.AddComponent<NavMeshObstacle>();
            obs.shape = NavMeshObstacleShape.Box;
            obs.center = Vector3.zero;
            obs.size = new Vector3(1f, 1f, 6f);   // algo mas gruesa que la hoja: el corte cubre todo el grosor del hueco
            obs.carving = true;
            DoorLeaves.Add(hinge.gameObject);
            return hinge.gameObject.AddComponent<Door>();
        }

        /// <summary>Estanteria metalica abierta de 4 baldas (y de las superficies: 0.135, 0.615, 1.095, 1.575), con cajas de carton.</summary>
        static GameObject MetalShelf(Transform parent, Material metal, Vector3 pos, float yaw)
        {
            var root = new GameObject("MetalShelf");
            root.transform.SetParent(parent);
            foreach (float x in new[] { -0.58f, 0.58f })
                foreach (float z in new[] { -0.2f, 0.2f })
                    Box("Post", new Vector3(x, 0.95f, z), new Vector3(0.04f, 1.9f, 0.04f), metal, root.transform);
            var board = Mat("ShelfBoard", new Color(0.36f, 0.36f, 0.34f));
            var card = Mat("Cardboard", new Color(0.45f, 0.35f, 0.22f));
            float[] ys = { 0.12f, 0.6f, 1.08f, 1.56f };
            int seed = Mathf.RoundToInt(pos.x * 7f + pos.z * 3f);
            var rnd = new System.Random(seed);
            foreach (float y in ys)
            {
                Box("Board", new Vector3(0f, y, 0f), new Vector3(1.2f, 0.03f, 0.45f), board, root.transform);
                // cajas de carton dejando huecos (en la balda de 1.08 queda sitio libre en el centro)
                foreach (float x in new[] { -0.4f, 0.4f })
                {
                    if (rnd.NextDouble() < 0.3) continue;
                    float h = 0.18f + (float)rnd.NextDouble() * 0.2f;
                    var bx = Box("Box", new Vector3(x + ((float)rnd.NextDouble() - 0.5f) * 0.1f, y + 0.015f + h / 2f, ((float)rnd.NextDouble() - 0.5f) * 0.08f),
                                 new Vector3(0.32f, h, 0.3f), card, root.transform);
                    bx.transform.localRotation = Quaternion.Euler(0f, ((float)rnd.NextDouble() - 0.5f) * 20f, 0f);
                }
            }
            root.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            return root;
        }
        /// <summary>Manchas de sangre: planos con recorte por alfa, apenas sobre el suelo y las paredes.</summary>
        static void BuildDecals(Transform details)
        {
            void Decal(Vector3 pos, Vector3 euler, float size, int variant)
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>($"{Mats}/Decal_Blood{variant}.mat");
                if (m == null) return;
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "Decal_Blood";
                q.transform.SetParent(details);
                q.transform.position = pos;
                q.transform.rotation = Quaternion.Euler(euler);
                q.transform.localScale = new Vector3(size, size, 1f);
                q.GetComponent<Renderer>().sharedMaterial = m;
                Object.DestroyImmediate(q.GetComponent<Collider>());
            }
            // Suelo (Quad mira hacia -Z: giro de 90 en X para que mire hacia arriba)
            Decal(new Vector3(-6f, 0.012f, -2f), new Vector3(90f, 20f, 0f), 2.6f, 1);
            Decal(new Vector3(6f, 0.012f, -4f), new Vector3(90f, 140f, 0f), 2.0f, 2);
            Decal(new Vector3(0.5f, 0.012f, 0.5f), new Vector3(90f, 75f, 0f), 2.2f, 3);
            Decal(new Vector3(-2f, 0.012f, -9.5f), new Vector3(90f, 200f, 0f), 1.5f, 2);
            Decal(new Vector3(11f, 0.012f, -9f), new Vector3(90f, 310f, 0f), 2.1f, 1);
            Decal(new Vector3(-4.5f, 0.012f, 6.5f), new Vector3(90f, 35f, 0f), 1.8f, 3);
            Decal(new Vector3(6f, 0.012f, 10f), new Vector3(90f, 250f, 0f), 2.4f, 1);
            Decal(new Vector3(-6f, 0.012f, 14f), new Vector3(90f, 110f, 0f), 2.0f, 2);
            // Paredes (mirando hacia la sala)
            Decal(new Vector3(-3.5f, 1.5f, 4.485f), new Vector3(0f, 0f, 5f), 1.8f, 2);        // pared interior, lado principal
            Decal(new Vector3(-14.48f, 1.4f, -1f), new Vector3(0f, 270f, 0f), 1.6f, 1);        // pared oeste (mira hacia +X)
            Decal(new Vector3(6f, 1.4f, 5.515f), new Vector3(0f, 180f, 0f), 1.7f, 3);          // pared interior, lado del fondo
        }

        const string WeaponsPath = "Assets/_Project/Art/Weapons/";
        const string PropsPath = "Assets/_Project/Art/Props/";

        /// <summary>Modelo, escala y masa con los que el objeto aparece en el suelo (con fisicas).</summary>
        static void SetWorld(ItemData item, string fbx, float scale, float mass)
        {
            item.worldPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WeaponsPath + fbx + ".fbx");
            item.worldScale = scale;
            item.mass = mass;
            EditorUtility.SetDirty(item);
        }

        /// <summary>Material de entorno con textura; si no existe, el de reserva (plano).</summary>
        static Material Env(string name, Material fallback)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>($"{Mats}/{name}.mat");
            return m != null ? m : fallback;
        }

        /// <summary>
        /// Cubo con UV de escala real: la textura se repite cada 'tile' metros en cada cara, alineada al mundo,
        /// de modo que una pared de 30 m y otra de 2 m tienen el mismo tamano de baldosa o veta.
        /// </summary>
        static Mesh TiledUnitCube(Vector3 pos, Vector3 size, float tile)
        {
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            void Face(Vector3 n, Vector3 u, Vector3 v)
            {
                float su = Vector3.Dot(size, u), sv = Vector3.Dot(size, v);
                float pu = Vector3.Dot(pos, u), pv = Vector3.Dot(pos, v);
                var corners = new[]
                {
                    n * 0.5f - u * 0.5f - v * 0.5f, n * 0.5f + u * 0.5f - v * 0.5f,
                    n * 0.5f + u * 0.5f + v * 0.5f, n * 0.5f - u * 0.5f + v * 0.5f,
                };
                int b = verts.Count;
                foreach (var c in corners)
                {
                    verts.Add(c);
                    norms.Add(n);
                    uvs.Add(new Vector2((Vector3.Dot(c, u) * su + pu) / tile, (Vector3.Dot(c, v) * sv + pv) / tile));
                }
                bool flip = Vector3.Dot(Vector3.Cross(u, v), n) < 0f;
                int[] order = flip ? new[] { 0, 2, 1, 0, 3, 2 } : new[] { 0, 1, 2, 0, 2, 3 };
                foreach (int i in order) tris.Add(b + i);
            }

            Face(Vector3.right, Vector3.forward, Vector3.up);
            Face(Vector3.left, Vector3.forward, Vector3.up);
            Face(Vector3.up, Vector3.right, Vector3.forward);
            Face(Vector3.down, Vector3.right, Vector3.forward);
            Face(Vector3.forward, Vector3.right, Vector3.up);
            Face(Vector3.back, Vector3.right, Vector3.up);

            var mesh = new Mesh { name = "TiledCube" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        static GameObject Box(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent, float tile = 0f, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (tile > 0f) go.GetComponent<MeshFilter>().sharedMesh = TiledUnitCube(pos, scale, tile);
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static T Asset<T>(string name, System.Action<T> init) where T : ScriptableObject
        {
            string path = $"{Data}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;
            var a = ScriptableObject.CreateInstance<T>();
            init(a);
            AssetDatabase.CreateAsset(a, path);
            return a;
        }

        static Material Mat(string name, Color color)
        {
            string path = $"{Mats}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }
    }
}
#endif
