# 🎭 TheaterCue

Aplicación de escritorio para gestión y reproducción de cues de audio en producciones teatrales, desarrollada como proyecto académico.

---

## 🎓 Información Académica

| Campo | Detalle |
|---|---|
| **Autor** | Yansel Martinez |
| **Carrera** | Ingeniería en Tecnologías de la Información y la Comunicación (TIC) |
| **Universidad** | UNIBE – Universidad Iberoamericana |
| **Año** | 2026 |

---

## 🧰 Stack Tecnológico

| Capa | Tecnología |
|---|---|
| UI | .NET MAUI Blazor Hybrid |
| Motor de audio | NAudio (WasapiOut, AudioFileReader) |
| Arquitectura | Clean Architecture |
| Persistencia | JSON (.cueshow) |
| Plataforma | Windows 10/11 |

---

## 🏗️ Estructura del Proyecto

```
TheaterCue-Solution/
├── TheaterCue/                          # UI — .NET MAUI Blazor Hybrid
│   ├── Components/
│   │   ├── Pages/
│   │   │   ├── Home.razor               # Grid principal de cues
│   │   │   └── EnvelopeEditor.razor     # Editor de automatización de volumen
│   │   ├── Layout/
│   │   │   └── MainLayout.razor
│   │   ├── CueGrid.razor                # Grid de tarjetas con drag & drop
│   │   └── CuePlayerCard.razor          # Tarjeta individual de pista
│   └── wwwroot/
│       ├── css/app.css
│       ├── images/
│       │   └── logo-unibe.png
│       └── js/
│           ├── seek.js                  # Control de posición del slider
│           └── dragdrop.js              # Drag & drop y splitter
│
├── TheaterCue.Domain/                   # Entidades del dominio
│   ├── CueTrack.cs
│   ├── VolumeEnvelope.cs
│   ├── AutomationNode.cs
│   ├── GridPosition.cs
│   └── ShowProject.cs
│
├── TheaterCue.Application/              # Contratos e interfaces
│   ├── IAudioEngine.cs
│   ├── IProjectRepository.cs
│   ├── PlaybackSnapshot.cs
│   ├── PlaybackState.cs
│   └── ShowStateService.cs
│
├── TheaterCue.Infrastructure.Audio/     # Implementaciones de NAudio
│   ├── NAudioEngine.cs
│   ├── EnvelopeSampleProvider.cs
│   └── JsonProjectRepository.cs
│
└── TheaterCue.AudioPoc/                 # Prueba de concepto original (consola)
    └── Program.cs
```

```
TheaterCue-Solution/
    TheaterCue/                          (UI - .NET MAUI Blazor Hybrid)
        Components/
            Pages/
                Home.razor               (Grid principal de cues)
                EnvelopeEditor.razor     (Editor de automatización de volumen)
            Layout/
                MainLayout.razor
            CueGrid.razor                (Grid de tarjetas con drag and drop)
            CuePlayerCard.razor          (Tarjeta individual de pista)
        wwwroot/
            css/app.css
            images/
                logo-unibe.png
            js/
                seek.js                  (Control de posicion del slider)
                dragdrop.js              (Drag and drop y splitter)

    TheaterCue.Domain/                   (Entidades del dominio)
        CueTrack.cs
        VolumeEnvelope.cs
        AutomationNode.cs
        GridPosition.cs
        ShowProject.cs

    TheaterCue.Application/              (Contratos e interfaces)
        IAudioEngine.cs
        IProjectRepository.cs
        PlaybackSnapshot.cs
        PlaybackState.cs
        ShowStateService.cs

    TheaterCue.Infrastructure.Audio/     (Implementaciones de NAudio)
        NAudioEngine.cs
        EnvelopeSampleProvider.cs
        JsonProjectRepository.cs

    TheaterCue.AudioPoc/                 (Prueba de concepto original)
        Program.cs
```
---

## ✨ Funcionalidades

### Reproducción de Audio
- Reproducción multi-pista simultánea con baja latencia (WASAPI, 50ms)
- Play, Pause y Stop por pista individual
- Seek (adelantar/atrasar) con bloqueo de seguridad para operación en vivo
- Soporte de formatos: MP3, WAV, FLAC, OGG, AAC

### Control de Volumen
- Fader de volumen manual por pista en tiempo real
- Fade In automático (5 segundos)
- Fade Out automático (8 segundos)
- Cancelación automática del fade al intervenir manualmente

### Automatización
- Editor visual de curva de automatización de volumen (SVG interactivo)
- Agregar nodos con click, mover con drag, eliminar con doble click
- Cabezal de reproducción animado en tiempo real sobre la curva
- Interpolación lineal entre nodos

### Vúmetro
- Barra de nivel RMS animada a 30fps
- Indicador de peak independiente
- Gradiente verde → amarillo → rojo

### Grid de Cues
- Dos columnas: Música Principal y Efectos de Sonido (SFX)
- Drag & drop entre columnas y dentro de la misma columna
- Splitter redimensionable entre columnas
- Agregar y eliminar tarjetas dinámicamente

### Proyecto
- Guardar y abrir proyectos en formato `.cueshow` (JSON)
- Estado del show persistente durante la navegación
- Nombre editable por tarjeta

---

## 🚀 Requisitos

- Windows 10 versión 1903 o superior / Windows 11
- .NET 8 SDK
- Visual Studio 2022 (17.8+) con la carga de trabajo **.NET MAUI**
- Dispositivo de audio compatible con WASAPI

---

## ▶️ Cómo ejecutar

1. Clona o descarga el repositorio
2. Abre `TheaterCue.AudioPoc.slnx` en Visual Studio 2022
3. Establece `TheaterCue` como proyecto de inicio
4. Presiona `F5` para compilar y ejecutar

---

## 📁 Formato de archivo `.cueshow`

Los proyectos se guardan como JSON con la extensión `.cueshow`:

```json
{
  "Name": "Mi Show",
  "Tracks": [
    {
      "Id": "...",
      "Name": "Apertura",
      "FilePath": "C:\\ruta\\al\\archivo.mp3",
      "Position": { "Row": 0, "Column": 0 },
      "ManualVolume": 1.0,
      "Envelope": { "Nodes": [] }
    }
  ],
  "SchemaVersion": 1
}
```

---

## 🏛️ Decisiones Arquitectónicas

**Clean Architecture**: La UI (Blazor) nunca depende de NAudio directamente. Toda la comunicación ocurre a través de `IAudioEngine`, lo que permite testear la lógica con mocks y cambiar el motor de audio sin tocar la UI.

**Thread Safety**: El hilo de audio de NAudio y el hilo de UI de Blazor nunca se bloquean entre sí. La UI hace polling del estado a 30fps mediante `PeriodicTimer` + `InvokeAsync(StateHasChanged)`, leyendo propiedades `volatile` del `EnvelopeSampleProvider`.

**Inmutabilidad**: `VolumeEnvelope` y `AutomationNode` son inmutables. Cada edición crea una nueva instancia, garantizando que el hilo de audio siempre lee una referencia consistente sin necesidad de locks.

**Singleton de Audio**: `NAudioEngine` vive como Singleton durante toda la vida de la app, sobreviviendo la navegación entre páginas de Blazor.

---

## 📄 Licencia

Proyecto académico — Universidad Iberoamericana (UNIBE) © 2026
