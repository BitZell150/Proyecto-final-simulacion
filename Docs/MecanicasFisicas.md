# Mecánicas de física (híbrido)

Los niveles, las reglas y los controles son los de `Docs/DisenoNiveles.md`. Lo que cambia es **quién mueve los
objetos**: el jugador y la caja ya no usan la física de Unity (Rigidbody2D dinámico), sino física calculada por
código, igual que la flecha. Unity solo se usa para *detectar* contra qué choca cada objeto.

| Objeto | Antes | Ahora |
|---|---|---|
| Jugador | Rigidbody2D dinámico (Unity integra) | `PhysicsBody2D`: Euler semi-implícito por código |
| Caja | Rigidbody2D dinámico + material con fricción | `PhysicsBody2D` + `PushableBox`: rozamiento de Coulomb por código |
| Flecha | Por código (RK4 / Euler) | Igual, sin cambios |
| Proyectil del cañón | No existía | `Projectile`: tiro parabólico por código, con aire y viento |

Los controles no cambian: A/D mover, Espacio saltar, G invertir, Q/E magnitud de |g|, F disparar, T trayectoria,
R reiniciar. Tampoco cambian los valores de juego (v = 8 u/s, salto de 12 u/s, escala de gravedad 5), así que
las alturas y largos de salto con los que se diseñaron los niveles son los mismos.

## Integración (jugador y caja): `PhysicsBody2D`

En cada `FixedUpdate` (Δt = 0,02 s) se aplica la segunda ley de Newton y el método de Euler semi-implícito,
el mismo orden de los talleres (primero la velocidad, luego la posición):

```
a = k·g + ΣF / m - (c / m)·v
v(t + Δt) = v(t) + a·Δt
x(t + Δt) = x(t) + v(t + Δt)·Δt
```

- `g` es la gravedad del juego. `GravityController` la cambia (sentido y magnitud) y la guarda en
  `Physics2D.gravity`, que aquí es solo una variable compartida: como ningún cuerpo es dinámico, Unity no la
  aplica a nada. La leen el jugador, la caja, la flecha y los proyectiles.
- `k` es la escala de gravedad y `m` la masa. Las dos se leen del `Rigidbody2D` del objeto (que queda en modo
  *Kinematic*), así se siguen editando en el mismo sitio del Inspector.
- Para no atravesar paredes, el desplazamiento se hace primero en x y luego en y, lanzando la caja del collider
  (`Physics2D.BoxCast`) en la dirección del movimiento. Si hay un obstáculo, el cuerpo avanza solo hasta tocarlo
  y la velocidad contra él se resuelve con `v' = v_obstáculo - e·(v - v_obstáculo)` (e = restitución, 0 por defecto).

`PlayerController2D` conserva su lectura de teclado y mando, las animaciones y los sonidos; solo cambia que
escribe la velocidad en `PhysicsBody2D` y llama a `Step`. El componente se añade solo al jugador al iniciar.

## Gravedad: `GravityController`

Es el control de los niveles (invertir y cambiar |g| entre 0,5·g₀ y 1,5·g₀, solo con los pies en el suelo).
Se le añadió un límite opcional de inversiones:

- `maxInversions = 0` (valor de los tres niveles): sin límite, como siempre.
- `maxInversions > 0`: solo se puede invertir esa cantidad de veces; el HUD muestra los usos y las recargas
  (`GravityRecharge`) devuelven usos. La sala de pruebas usa 3.

## Empuje de la caja: `PushableBox`

Rozamiento seco (modelo de Coulomb) con la superficie donde se apoya (suelo, o techo con g invertida):

```
N = m·|g|·k
En reposo:   no se mueve mientras |F| ≤ μs·N          (μs = 0,5)
Deslizando:  f = -μk·N·signo(v)                        (μk = 0,4)
a = (F + f) / m
```

La fuerza del jugador es como la de un motor: máxima con la caja quieta y cero a su rapidez de caminar.

```
F = F_max·(1 - v_caja / v_caminar)        F_max = 250 N, v_caminar = 8 u/s
```

La caja termina a la rapidez en que F iguala al rozamiento cinético: `v = v_caminar·(1 - μk·m·|g|·k / F_max)`.
Para la caja de los niveles (1 kg, k = 5) son 7,4 u/s con g₀, prácticamente lo mismo que antes. Como N depende
de |g|, al bajar la gravedad la caja roza menos, y una caja muy pesada solo se mueve con poca gravedad.

## Proyectiles del cañón: `ProjectileLauncher` y `Projectile`

Modelo del taller de movimiento parabólico, con rozamiento lineal relativo al viento:

```
a = g - (c / m)·(v - v_viento)
```

Con `c = 0` es la parábola ideal: `r(t) = r0 + v0·t + ½·g·t²`, alcance `R = v0²·sen(2θ) / g`.

- El lanzador dispara cada `fireInterval` segundos con rapidez `launchSpeed` y ángulo `launchAngle`.
- Con `aimAtPlayer` calcula el ángulo para pasar por el jugador despejando θ de la trayectoria:
  `tanθ = (v² ± √(v⁴ - g·(g·x² + 2·y·v²))) / (g·x)` (− tiro bajo, + tiro alto). Usa la gravedad actual, así que
  también apunta bien con la gravedad invertida o con otra magnitud. Fuera de alcance usa el ángulo fijo.
- En la vista Scene se dibuja la trayectoria prevista (naranja) y el punto más alto (amarillo).
- Choques: contra el jugador se comprueba a mano círculo contra caja; si lo toca, el nivel se reinicia
  (`LevelManager.KillPlayer`). Contra lo sólido se usa un `CircleCast`. Si golpea una caja le transmite velocidad
  con la fórmula del choque elástico del taller de colisiones.

## Sala de pruebas: `Assets/Scenes/PruebasFisica.unity`

No forma parte de la ruta del juego (ningún menú lleva a ella): se abre desde el editor y se le da Play.
Tiene el arte de los niveles y:

| Qué | Para ver |
|---|---|
| Cajas de 1, 4 y 15 kg | La rapidez al empujar depende de la masa. La de 15 kg no arranca con g₀ (μs·m·g = 368 N > 250 N) pero sí con \|g\| = 0,5·g₀ (184 N): pulsar Q dos veces |
| Cañón | Apunta al jugador cuando está dentro de su alcance (≈ 14,7 u con g₀) |
| Gravedad con 3 inversiones y 2 recargas | El límite de usos |
| Puerta detrás del cañón | Llegar hasta ella termina la sala |

Se regenera con **Niveles > Construir Pruebas de física**. Para usar el cañón o las recargas en un nivel,
arrastrar los prefabs `Lanzador` y `RecargaGravedad` de `Assets/Niveles/Prefabs`.

## Pruebas automáticas

`Window > General > Test Runner > PlayMode > Run All`. Además de las pruebas de los tres niveles,
`PruebasFisicaTests` comprueba la sala: rapidez de las cajas según la fórmula, caja pesada que solo se mueve con
menos gravedad, límite de inversiones y recarga, parábola del proyectil y que el cañón alcanza al jugador.

## Archivos

- `Assets/Scripts/Fisica/PhysicsWorld.cs`: gravedad del juego y fórmulas compartidas.
- `Assets/Scripts/Fisica/PhysicsBody2D.cs`: integración y choques de jugador y cajas.
- `Assets/Scripts/Fisica/PushableBox.cs`: rozamiento de la caja.
- `Assets/Scripts/Fisica/Ballistics.cs`, `Projectile.cs`, `ProjectileLauncher.cs`: cañón y tiro parabólico.
- `Assets/Scripts/Fisica/GravityRecharge.cs`: recarga de inversiones.
- `Assets/pesonaje/movimiento personaje.cs` (`PlayerController2D`) y `Assets/Niveles/Scripts/GravityController.cs`:
  adaptados para usar la física por código.
- `Assets/Niveles/Editor/PruebasFisicaBuilder.cs` y `Assets/Niveles/Tests/PruebasFisicaTests.cs`: sala de pruebas.
