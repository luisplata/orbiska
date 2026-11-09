# Orbiska — Kit de Play Store (ES + EN)

Listo para pegar en Play Console. Lo único que falta son los binarios de arte (WU4, BLOCKED-USER) y la creación de la app (WU7).

## Identidad de la app

| Campo | Valor |
|---|---|
| Nombre | Orbiska |
| Package | `com.bellseboss.orbiska` |
| versionName | `0.0.1` |
| versionCode | `1` |
| targetSdk | 36 (esperado; verificar en el AAB) |
| minSdk | 25 |
| Publisher | Bellseboss Studio |
| Email de soporte | support@bellseboss.com |
| Categoría | Juegos → Arcade |
| Precio | Gratis, sin compras integradas |
| Idiomas | ES (principal), EN |

## Listing

### Título

Title (EN): Orbiska - Missile Defense
- caracteres: 25/30

Title (ES): Orbiska - Defensa Antimisil
- caracteres: 27/30

### Descripción corta

Short (EN): Hold to fire interceptors. Keep six cities alive through five waves.
- caracteres: 68/80

Short (ES): Mantén pulsado para disparar y protege seis ciudades durante cinco oleadas.
- caracteres: 75/80

## Full description (EN) — 1444/4000 caracteres

Orbiska is an arcade missile-defense game. Six cities sit under your protection, three interceptor batteries stand ready, and the sky fills with incoming missiles.

AIM, FIRE, SURVIVE
Hold the left mouse button, or hold a finger on the screen, to aim. Every shot comes from the battery closest to your cursor that still has ammo. Each battery carries 10 interceptors, so every miss costs you a city.

FIVE WAVES, THREE MISSILE TYPES
- Normal missiles fall straight toward your cities.
- MIRVs split into three warheads halfway down.
- Smart missiles dodge your explosion clouds and re-aim at a city that is still standing.

Each wave brings more missiles and more speed. Clear a wave and the sky calms down for a moment.

PICK AN UPGRADE
Between waves you choose 1 of 3 cards from a pool of five upgrades:
- Double: two interceptors per shot.
- Slow: enemy missiles crawl for a few seconds.
- Thrusters: faster interceptors and faster aiming.
- Radius+: a wider explosion cloud.
- Reload: shorter cooldown between shots.

WIN OR LOSE
Every intercept scores points. Lose all six cities and the run ends. Clear five waves with at least one city alive and you win.

BUILT TO BE SIMPLE
- No sign-in, no ads, no analytics.
- Fully offline: the game never contacts a server.
- Runs in the browser and on Android.
- Nothing is stored between runs.

Orbiska is a short, focused arcade game: manage your ammo, read the sky, and keep one more city alive.


## Full description (ES) — 1519/4000 caracteres

Orbiska es un juego arcade de defensa antimisil. Seis ciudades bajo tu protección, tres baterías de interceptores y un cielo que se llena de misiles.

APUNTA, DISPARA, SOBREVIVE
Mantén pulsado el botón izquierdo del ratón, o el dedo sobre la pantalla, para apuntar. Cada disparo sale de la batería con munición más cercana al puntero. Cada batería lleva 10 interceptores, así que cada fallo te cuesta una ciudad.

CINCO OLEADAS, TRES TIPOS DE MISIL
- Misiles normales: caen directos hacia tus ciudades.
- MIRV: se dividen en tres cabezas a mitad de camino.
- Misiles inteligentes: esquivan tus nubes de explosión y vuelven a apuntar a una ciudad en pie.

Cada oleada trae más misiles y más velocidad. Si despejas el cielo, tendrás un respiro.

ELIGE UNA MEJORA
Entre oleadas eliges 1 de 3 cartas de un grupo de cinco mejoras:
- Doble: dos interceptores por disparo.
- Lenta: los misiles enemigos se frenan unos segundos.
- Propulsores: interceptores y apuntado más rápidos.
- Radio+: nube de explosión más amplia.
- Recarga: menos espera entre disparos.

GANAR O PERDER
Cada intercepción suma puntos. Si pierdes las seis ciudades, la partida termina. Supera cinco oleadas con al menos una ciudad en pie y ganas.

SIN COMPLICACIONES
- Sin registro, sin anuncios, sin analíticas.
- Totalmente sin conexión: el juego nunca contacta con un servidor.
- Funciona en navegador y en Android.
- No se guarda nada entre partidas.

Orbiska es un arcade breve y directo: administra la munición, lee el cielo y salva una ciudad más.


## What's new (EN) — 346/500 caracteres

First release as Orbiska.

- New name and a new Android package (com.bellseboss.orbiska).
- Six cities, three interceptor batteries, 10 shots each.
- Five waves with normal missiles, MIRVs and smart missiles.
- Pick 1 of 3 upgrade cards between waves: Double, Slow, Thrusters, Radius+ or Reload.
- Fully offline. No sign-in, no ads, no analytics.


## Gráficos requeridos

Estado: **PENDIENTE — WU4 (BLOCKED-USER)**. Las rutas y especificaciones quedan fijadas acá.

| Asset | Archivo | Especificación | Estado |
|---|---|---|---|
| Icono | `PlayStore/assets/icon-512.png` | PNG 512×512, 32 bits con alpha, ≤1024 KB | PENDIENTE |
| Feature graphic | `PlayStore/assets/feature-1024x500.png` | 1024×500, sin alpha (24 bits), texto dentro del 80% central | PENDIENTE |
| Captura 1 | `PlayStore/assets/screenshots/shot-1.png` | 1080×1920 | PENDIENTE |
| Captura 2 | `PlayStore/assets/screenshots/shot-2.png` | 1080×1920 | PENDIENTE |
| Captura 3 | `PlayStore/assets/screenshots/shot-3.png` | 1080×1920 | PENDIENTE |
| Captura 4 | `PlayStore/assets/screenshots/shot-4.png` | 1080×1920 | PENDIENTE |
| Captura 5 | `PlayStore/assets/screenshots/shot-5.png` | 1080×1920 | PENDIENTE |
| Captura 6 | `PlayStore/assets/screenshots/shot-6.png` | 1080×1920 | PENDIENTE |

Reglas de Play: mínimo 4 capturas de teléfono, cada lado entre 320 y 3840 px, proporción máxima 2:1, PNG o JPEG. Los archivos de acá son 1080×1920 (9:16), así que cumplen.

### Capturas y textos (≤140 caracteres cada uno)

| # | Estado capturado | Caption (EN) | Caption (ES) |
|---|---|---|---|
| 1 | Menú principal | "Three batteries, six cities, five waves." | "Tres baterías, seis ciudades, cinco oleadas." |
| 2 | Inicio de oleada | "The sky fills up fast. Hold to fire." | "El cielo se llena rápido. Mantén para disparar." |
| 3 | Intercepción a media oleada | "One shot per battery tick. Make it count." | "Un disparo por turno de batería. Que valga." |
| 4 | Draft de cartas | "Pick 1 of 3 upgrades between waves." | "Elige 1 de 3 mejoras entre oleadas." |
| 5 | Ciudades dañadas | "Every city you lose is one less shield." | "Cada ciudad perdida es un escudo menos." |
| 6 | Game over / victoria | "Clear five waves or start over." | "Supera cinco oleadas o empieza de nuevo." |

## Data safety (Play Console)

| Pregunta | Respuesta |
|---|---|
| ¿La app recopila o comparte datos de usuario? | No. No recopila ni comparte ningún dato. |
| Tipos de datos | Ninguno (sin ubicación, sin información personal, sin actividad en la app, sin datos financieros) |
| ¿Datos cifrados en tránsito? | No aplica: no se transmiten datos |
| ¿Los usuarios pueden pedir borrado? | No aplica: no se almacena nada del usuario |
| ¿La app es apta para familias? | No está dirigida a menores; ver "Audiencia" |
| Tracking / publicidad | Ninguno. Sin SDK de anuncios, sin analíticas (`submitAnalytics: 0`), sin crash reporting |
| Compras integradas | Ninguna |
| Permisos sensibles | Ninguno |

## Content rating (IARC)

| Pregunta | Respuesta |
|---|---|
| Violencia | Acción abstracta de defensa antimisil; misiles y explosiones sin sangre, sin gore, sin cuerpos humanos |
| Personajes humanos | Ninguno |
| Contenido generado por usuarios | No |
| Chat / interacción social | No |
| Anuncios | No |
| Compras | No |
| Drogas, alcohol, tabaco, apuestas | No |
| Lenguaje | No |
| Ubicación compartida | No |
| Clasificación sugerida a declarar | Violencia leve / fantasía → dejar que IARC determine la etiqueta final |

## Audiencia y contenido

- Audiencia objetivo: 13+ (no dirigida a niños). No marcar "Diseñada para familias".
- Sin UGC, sin ads, sin compras, sin recopilación de datos: no aplican requisitos adicionales de familias.

## URLs y soporte

| Campo | Valor |
|---|---|
| Política de privacidad | `https://luisplata.github.io/orbiska/privacy-policy.html` |
| Términos de uso | `https://luisplata.github.io/orbiska/terms.html` |
| Sitio web | `https://luisplata.github.io/orbiska/` |
| Email de soporte | support@bellseboss.com |

Nota: esas URLs responden 200 recién después de WU7 (rename del repo + Pages).

## Checklist de envío

- [ ] AAB `Orbiska-0.0.1.aab` generado por el workflow *Release PlayStore* con `series=0.0`, `bump=patch`.
- [ ] Manifest verificado: `package=com.bellseboss.orbiska`, `versionName=0.0.1`, `versionCode=1`, `targetSdkVersion=36`.
- [ ] Huella SHA-256 del certificado de subida registrada (mismo keystore de siempre, sin rotación).
- [ ] Icono, feature graphic y 6 capturas subidos con las especificaciones de la tabla de arriba.
- [ ] Listing ES y EN pegados con los conteos de caracteres indicados.
- [ ] Data safety y content rating completados con las respuestas de este documento.
- [ ] URLs de privacidad y términos respondiendo 200.
- [ ] Email de soporte y sitio web cargados.
- [ ] Internal testing primero; producción recién después de aprobar la revisión.
- [ ] Listing viejo de Play se elimina SOLO después de que la nueva app esté aprobada (WU7).

### Evidencia (completar en WU7)

| Dato | Valor |
|---|---|
| SHA-256 del certificado | PENDIENTE (pegar salida de `keytool`) |
| Salida de `bundletool dump manifest` | PENDIENTE |
| Fecha de subida | PENDIENTE |
