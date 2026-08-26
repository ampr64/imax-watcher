# ImaxWatcher

Watcher en .NET 10 que vigila la boletería de **Showcase Cinemas** y avisa apenas
se habilitan funciones de una película posteriores a una fecha de corte.

El caso de uso: la boletería publica sólo una ventana corta de días. Cuando abren
la tanda siguiente, las buenas funciones vuelan. Este programa mira la página
cada 25–40 segundos y avisa por consola, mail, Telegram y WhatsApp en cuanto
aparece algo nuevo.

Está armado para un solo cine: el IMAX (Norcenter). Lo único que cambia de una
película a otra es **qué película** mirar y **desde qué fecha** avisar; eso sí
es configuración, en `appsettings.json`, sin tocar código.

---

## Cómo funciona

No busca una fecha como texto en el HTML. Reproduce el flujo real de la
boletería, que son cinco `<select>` encadenados por postback de ASP.NET:

| Paso | Control | Qué hace |
|------|---------|----------|
| 1 | `lstCinemaFull` | Elige el IMAX (Norcenter), fijo en `Watcher:Cinema` |
| 2 | `lstMovies` | Elige la película configurada en `Watcher:Movie` |
| 3 | `lstFormat` | Recorre todos los formatos realmente disponibles |
| 4 | `lstDays` | Lee el selector de días y descarta los que no superan `CutoffDate` |
| 5 | `lstPerf` | Para cada día candidato, lee los horarios `HH:mm` |

Sólo dispara alerta si logra extraer **al menos un horario real** del paso 5. Una
fecha que aparece en el selector pero todavía no tiene horarios cargados no
cuenta como resultado válido: eso evita el falso positivo típico de avisar por una fecha que
todavía no se puede comprar.

La búsqueda de horarios está acotada al `<select>` del paso 5 a propósito.
Barrer la página entera buscando `HH:mm` convertiría cualquier `10:00 a 22:00`
de un pie de página en una función inexistente.

### Qué pasa cuando encuentra una función nueva

1. Imprime el detalle en consola y hace sonar una alarma local.
2. Abre la boletería en el navegador (configurable).
3. Manda las notificaciones remotas habilitadas, en paralelo.

Reglas de entrega:

- Una función se marca como avisada **sólo si algún canal remoto confirmó el
  envío**. Si fallan todos, no se marca y se reintenta en el ciclo siguiente.
- La alarma local (beeps + pestaña del navegador) se dispara **una sola vez por
  función**, aunque el envío remoto se reintente.
- Una función ya avisada nunca se vuelve a mandar.
- Con `StopAfterFirstMatch: true` el proceso termina después del primer aviso
  entregado. Si no se pudo entregar, sigue vivo reintentando.

---

## Requisitos

- .NET 10 SDK
- Chromium de Playwright (se instala en el setup)
- PowerShell 7 recomendado (funciona con Windows PowerShell 5)

---

## Setup

```powershell
git clone <repo>
cd ImaxWatcher
dotnet restore
dotnet build
pwsh .\bin\Debug\net10.0\playwright.ps1 install chromium
```

Si sólo tenés Windows PowerShell 5:

```powershell
powershell -ExecutionPolicy Bypass -File .\bin\Debug\net10.0\playwright.ps1 install chromium
```

> **Importante.** Ese comando baja **dos** binarios: *Chrome for Testing* y
> *Chrome Headless Shell*. El modo headless (el default) usa el segundo, así que
> si falta, la app corta con `Executable doesn't exist`.
>
> Volvé a correrlo cada vez que subas la versión del paquete
> `Microsoft.Playwright`: la build del navegador está atada a la del paquete y
> una no sirve para la otra.

Verificá que quedó andando:

```powershell
dotnet run -- --once
```

---

## Configuración

`Cinema` y `BookingUrl` vienen fijos al IMAX (Norcenter) y no hace falta
tocarlos. Lo único que cambia de una corrida a otra es **qué película** mirar
y **desde qué fecha** avisar:

```json
"Watcher": {
  "Cinema": "IMAX Theatre (Norcenter)",
  "Movie": "La Odisea",
  "CutoffDate": "2026-09-02",
  "BookingUrl": "https://www.voyalcine.net/showcase/boleteria.aspx"
}
```

- **`Movie`** tiene que coincidir con el texto de la opción en el sitio. El
  la coincidencia es exacta primero, ignorando acentos y mayúsculas; si no encuentra, cae
  a coincidencia parcial. Conviene usar el nombre completo: un parcial ambiguo
  elige el primero del selector y puede no ser el que querés.
- **`CutoffDate`** es la última fecha que **ya** viste publicada. El watcher
  avisa por fechas **estrictamente posteriores** (`>`, no `>=`). La forma
  práctica de elegirla: corré `--once`, mirá hasta qué día llega la boletería, y
  poné ese día.

Si la película no existe (cambió el texto en el sitio o salió de cartel), el
error de arranque lista lo que sí encontró en el selector, así que copiar y
pegar el nombre correcto es directo.

### Resto de los settings

| Setting | Default | Qué es |
|---------|---------|--------|
| `PollMinSeconds` / `PollMaxSeconds` | `25` / `40` | Intervalo con jitter aleatorio (mínimo 10) |
| `ErrorRetrySeconds` | `15` | Espera tras un error antes de reintentar |
| `SettleDelayMilliseconds` | `1500` | Espera tras cada postback (mínimo 100) |
| `NavigationTimeoutSeconds` | `30` | Timeout de navegación |
| `Headless` | `true` | Chromium invisible |
| `OpenBrowserOnMatch` | `true` | Abre la boletería al detectar una función nueva |
| `StopAfterFirstMatch` | `true` | Termina después del primer aviso entregado |

La configuración se lee desde el directorio del ejecutable, no desde el
directorio actual, así que el watcher se puede lanzar desde cualquier lado.

### Selectores

Si Showcase cambia el HTML, los selectores están todos juntos en
`Watcher:Selectors` para poder corregirlos sin tocar la lógica:

```json
"Selectors": {
  "Cinema":   "select#ctl00_Contenido_lstCinemaFull",
  "Movie":    "select#ctl00_Contenido_lstMovies",
  "Format":   "select#ctl00_Contenido_lstFormat",
  "Day":      "select#ctl00_Contenido_lstDays",
  "Showtime": "select#ctl00_Contenido_lstPerf"
}
```

`dotnet run -- --once --headed` abre Chromium visible para inspeccionarlos.

### Variables de entorno

Cualquier setting se puede sobreescribir con el prefijo `IMAX_` y `__` para
los niveles. Tienen prioridad sobre `appsettings.json` y sobre user-secrets:

```powershell
$env:IMAX_Watcher__Movie      = "Otra Pelicula"
$env:IMAX_Watcher__CutoffDate = "2026-09-10"
$env:IMAX_Telegram__Enabled   = "false"
```

---

## Canales de notificación

Los tres canales son independientes: se habilitan por separado y se envían en
paralelo. Si un canal está habilitado pero mal configurado, **el arranque corta
con un error que dice exactamente qué setting falta**, en vez de fallar recién en
el momento del aviso.

Las credenciales van **siempre** en user-secrets, nunca en `appsettings.json`.
Los comandos se corren desde la carpeta del proyecto.

### Email (Gmail)

Viene habilitado por default. Usá una **contraseña de aplicación** de Google, no
tu contraseña normal.

```powershell
dotnet user-secrets set "Email:From" "vos@gmail.com"
dotnet user-secrets set "Email:To" "vos@gmail.com,otra-persona@gmail.com"
dotnet user-secrets set "Email:Password" "TU_APP_PASSWORD"
```

`Email:To` acepta más de una dirección separadas por coma.

### Telegram

Viene deshabilitado. Hablale a [@BotFather](https://t.me/BotFather), creá un bot
y guardá el token. Para el `ChatId`, escribile algo a tu bot y mirá
`https://api.telegram.org/bot<TOKEN>/getUpdates`.

```powershell
dotnet user-secrets set "Telegram:BotToken" "123456789:AA..."
dotnet user-secrets set "Telegram:ChatId" "123456789"
dotnet user-secrets set "Telegram:Enabled" "true"
```

`ChatId` es numérico: si queda en `0`, el arranque falla con un mensaje
explícito. El mensaje incluye un botón **COMPRAR AHORA** que va derecho a la
boletería.

### WhatsApp (Twilio)

Viene deshabilitado. Primero uní tu WhatsApp al Sandbox de Twilio. Se autentica
con una **API Key** (`SK...`), no con el Auth Token de la cuenta:

```powershell
dotnet user-secrets set "WhatsApp:AccountSid" "ACxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
dotnet user-secrets set "WhatsApp:ApiKeySid" "SKxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
dotnet user-secrets set "WhatsApp:ApiKeySecret" "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
dotnet user-secrets set "WhatsApp:From" "+14155238886"
dotnet user-secrets set "WhatsApp:To" "+54911XXXXXXXX"
dotnet user-secrets set "WhatsApp:Enabled" "true"
```

#### Fuera de la ventana de 24 horas

WhatsApp exige un template para mensajes iniciados por la aplicación fuera de la
ventana de atención de 24 h. Si Twilio te dio un Content SID aprobado:

```powershell
dotnet user-secrets set "WhatsApp:ContentSid" "HXxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
```

Variables que manda el watcher: `{{1}}` fecha, `{{2}}` horarios, `{{3}}` URL de
compra. Si `ContentSid` queda vacío manda texto libre, que sirve dentro de la
ventana de 24 h; fuera de ella Twilio puede rechazarlo.

---

## Uso

| Comando | Qué hace |
|---------|----------|
| `dotnet run` | Monitorea en loop cada 25–40 s. `Ctrl+C` corta limpio |
| `dotnet run -- --once` | Un solo chequeo, imprime lo que encuentra y termina. **No notifica** |
| `dotnet run -- --once --headed` | Igual pero con Chromium visible, para depurar selectores |
| `dotnet run -- --test-notifications` | Manda un aviso de prueba con datos ficticios por todos los canales habilitados |

Conviene correr `--test-notifications` una vez después de configurar cada canal:
confirma que las credenciales realmente andan, sin esperar a que aparezcan
funciones.

---

## Resiliencia

Pensado para quedar corriendo desatendido durante días:

- **Si Chromium se cae**, lo detecta y lo relanza solo. Verificado matando el
  proceso del navegador con el watcher andando: se recupera en ~25 segundos.
- **Si falla un chequeo**, reintenta a los 15 segundos sin morir.
- **Si fallan todas las notificaciones**, no marca la función como avisada y
  reintenta en el ciclo siguiente, sin repetir la alarma local.
- **Si Showcase cambia el formato de fecha u hora**, lo avisa por log en vez de
  informar "sin funciones" para siempre, que sería el peor modo de falla posible
  para esto.

Para ver el detalle de cuántos días leyó, interpretó y pasaron el corte:

```powershell
$env:IMAX_Logging__LogLevel__ImaxWatcher = "Debug"
dotnet run
```

```
dbug: IMAX-Subtitulado: 8 días leídos, 8 interpretados, 0 posteriores al corte.
```

Esa línea distingue "todo bien, todavía no hay nada" de "se rompió el parser",
que desde afuera se ven igual.

> La máquina no puede suspenderse: si se duerme, el watcher deja de consultar.

---

## Estructura

```
Program.cs                     Composición: configuración, logging, DI, ciclo de vida
Extensions/                    Registro de servicios y formateo de mensajes
Models/                        Showing, WatchAlert
Options/                       Configuración tipada
  Validation/                  Atributos condicionales (RequiredIf, EmailAddressIf)
Notifications/                 INotifier + Gmail, Telegram, Twilio + orquestador
Services/
  ShowcaseReader.cs            Automatización del navegador (Playwright)
  WatcherRunner.cs             Ciclo de polling, dedupe y reintentos
  LocalAlertService.cs         Alarma local (consola, sonido, navegador)
  Parsing/ShowcaseParsing.cs   Parseo puro: fechas, horarios, placeholders
  Browser/ · Sound/            Implementaciones por sistema operativo
tests/                         Tests unitarios del parseo
```

---

## Tests

La lógica de parseo (fechas en español, horarios, filtrado de placeholders) está
separada del navegador en `Services/Parsing/ShowcaseParsing.cs` para poder
testearla sin levantar Chromium:

```powershell
dotnet test tests\ImaxWatcher.Tests
```

No hay archivo de solución: el proyecto de tests referencia al de la app
directamente, así que se corre apuntándolo por path.

---

## Diagnóstico

| Síntoma | Causa probable |
|---------|----------------|
| `Executable doesn't exist` | Falta el Headless Shell o la versión no coincide con el paquete. Recorré `playwright.ps1 install chromium` |
| `Configuración inválida:` al arrancar | Un canal habilitado sin configurar. El mensaje dice qué setting falta |
| `No encontré el cine` / `No encontré '<película>'` | Cambiaron los textos del sitio o la película salió de cartel. El error lista lo que sí encontró |
| `No pude interpretar N opciones de día` | Cambió el formato de fecha del sitio. Hay que ampliar `ShowcaseParsing.TryParseDate` |
| `El navegador dejó de responder` | Chromium se cayó. Es normal y se recupera solo |
| Nunca avisa | Verificá con `--once` que lee bien, y con `--test-notifications` que los canales andan |

---

## Nota sobre idiomas

El código, los comentarios y los nombres de tests están en inglés. La salida de
consola, los logs y el texto de las notificaciones están en español a propósito,
porque son lo que se lee cuando el watcher está corriendo. Este README también
está en español.
