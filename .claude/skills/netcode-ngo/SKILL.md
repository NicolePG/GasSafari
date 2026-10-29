---
name: netcode-ngo
description: Multijugador online con Netcode for GameObjects 1.x, Unity Transport, Relay, Lobby y Authentication (salas con código, balsa con autoridad del anfitrión, comandos de jugadores, replicación, desconexiones, Carrera por Equipos online).
---

# Online (NGO 1.x)

Diseño: docs/10 §14. Usa `[ServerRpc]`/`[ClientRpc]` y `NetworkVariable` (estilo clásico de 1.x).

## Pasos
1. **Servicios:** proyecto vinculado a Unity Gaming Services. `UnityServices.InitializeAsync()` → `AuthenticationService.Instance.SignInAnonymouslyAsync()`.
2. **Anfitrión:** crear asignación de Relay (3 conexiones en cooperativo, 7 en Carrera por Equipos) → obtener join code → crear Lobby con el código en sus datos → `UnityTransport.SetRelayServerData(...)` → `NetworkManager.StartHost()`.
3. **Cliente:** buscar Lobby por código → unirse a Relay → `StartClient()`.
4. **Escenas:** cargar niveles con `NetworkManager.SceneManager.LoadScene` (solo el anfitrión).
5. **Balsa:** `NetworkObject` + `NetworkRigidbody` + `NetworkTransform` (interpolación). Solo el anfitrión ejecuta física, flotación, corrientes, obstáculos y sync.
6. **Comandos:** cada cliente envía su `PlayerCommand` por tick (TickRate 30) con `[ServerRpc(Delivery = RpcDelivery.Unreliable)]`. El anfitrión lo entrega a `RemoteCommandSource`. Las pulsaciones son contadores: el anfitrión compara con el último valor visto.
7. **Sync con latencia:** tiempo del evento = hora de recepción − RTT/2 del cliente.
8. **Replicación visual:** asiento (`NetworkVariable<float>`), estado del avatar (`NetworkVariable<byte>`), gas (`NetworkVariable<byte>`). Efectos y sonidos con `ClientRpc` agrupados (un RPC por evento importante, nunca por frame).
9. **Desconexión:** `OnClientDisconnectCallback` → el `PlayerSlot` pasa a bot (`BotCommandSource`).
10. **Obstáculos:** solo el anfitrión los mueve; usar `NetworkTransform` en los que afectan la jugabilidad. Los decorativos se simulan localmente con la misma semilla.

## Errores comunes a evitar
- Aplicar física en clientes (la balsa debe ser kinematic en clientes).
- Usar `UnityEngine.Random` en lógica que deba coincidir entre máquinas.
- Enviar strings o listas por RPC en cada tick.

## Verificación
Network Simulator (Unity Transport) con 50, 150 y 300 ms y 2 % de pérdida: QA-11 y QA-12 de docs/11. Probar con dos instancias (build + editor).
