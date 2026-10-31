# Sonar Switcher

**Widget no oficial de Xbox Game Bar para cambiar los dispositivos de audio de SteelSeries Sonar, pensado para usarse con mando.**

[English](README.md) | **Español**

> **Aviso:** este es un proyecto personal de la comunidad. **No está afiliado, respaldado ni patrocinado por SteelSeries.**
> "SteelSeries" y "Sonar" son marcas de sus respectivos propietarios y se mencionan solo para indicar compatibilidad.
> Usa la API local de GG/Sonar, que **no es oficial ni está documentada**: puede dejar de funcionar con cualquier actualización de GG.
> Se ofrece tal cual, sin garantías (ver `LICENSE`).

## Para qué sirve

Si usas el PC conectado al televisor y manejas todo con un mando de Xbox, cambiar la salida de audio de Sonar
(por ejemplo, de la TV a los audífonos) obliga a usar teclado y ratón en la app de GG. Este widget vive dentro de
Game Bar (se abre con el botón Xbox del mando o con `Win + G`) y permite:

- Elegir **un solo dispositivo para todas las salidas** (Juego, Chat, Multimedia y Auxiliar).
- Elegir el dispositivo de **cada canal** por separado.
- Elegir el **micrófono**.

Muestra los valores actuales al abrirse y comprueba, después de cada cambio, que Sonar realmente los aplicó.

## Requisitos

- Windows 10 (1809 o superior) o Windows 11, con **Xbox Game Bar** activado.
- **SteelSeries GG con Sonar**, en **modo clásico** (el modo streamer no está soportado).
- Para compilar: **Visual Studio** con las herramientas de UWP (en VS 2026: carga de trabajo *Desarrollo de aplicaciones WinUI*
  con *Universal Windows Platform tools* y el SDK de Windows 11 10.0.26100).

Probado con unos Arctis Nova 7X en modo clásico, en octubre de 2026. No se ha probado con otros dispositivos ni versiones.

## Instalación (desde el código fuente)

1. Clona el repositorio y abre `SonarWidget.slnx` en Visual Studio.
2. Elige la plataforma **x64** y compila (`Ctrl + Shift + B`).
3. **Compilar > Implementar SonarWidget** (puede pedirte activar el modo desarrollador de Windows).
4. En PowerShell **como administrador**, ejecuta `exento-loopback.ps1`. Las apps UWP no pueden hablar con `localhost`
   si no se les concede una exención de red.
5. Con GG y Sonar abiertos, pulsa `Win + G`, abre el menú de widgets y busca **Sonar Switcher**.

El paquete NuGet `Microsoft.Gaming.XboxGameBar` se restaura solo al compilar.

## Cómo funciona

El widget habla con la API HTTP local que usa la propia app de GG:

1. `GET https://127.0.0.1:6327/subApps` (GG, con certificado autofirmado) entrega la dirección de Sonar. El puerto de Sonar
   cambia cada vez que GG se reinicia, así que el widget lo redescubre solo (botón **Actualizar**).
2. `GET /audioDevices` lista los dispositivos; los virtuales de Sonar se ocultan.
3. `GET /classicRedirections` indica qué dispositivo usa cada canal.
4. `PUT /classicRedirections/{canal}/deviceId/{id}` cambia el dispositivo de un canal (`game`, `chat`, `media`, `aux`, `mic`).

El puerto de GG (6327) es editable en las opciones avanzadas (engranaje) por si en tu equipo fuera distinto;
se puede consultar en `C:\ProgramData\SteelSeries\SteelSeries Engine 3\coreProps.json`, campo `ggEncryptedAddress`.

## Limitaciones conocidas

- Depende de una API no oficial; puede romperse sin aviso.
- Solo modo clásico de Sonar; no cambia presets (Quickset) ni volúmenes.
- La interfaz está en español.
- Solo se distribuye como código fuente: instalarlo fuera de la Microsoft Store requiere compilarlo y activar el modo desarrollador.

## Transparencia sobre el uso de IA

Este proyecto se desarrolló con asistencia de IA (Claude, de Anthropic): el código, la documentación y los íconos se
generaron con su ayuda. El autor definió el problema, dirigió el trabajo y probó el resultado en su propio equipo.
El código no ha tenido una auditoría independiente; revísalo antes de ejecutarlo si te importa.

## Licencia

MIT. Consulta el archivo `LICENSE`.
