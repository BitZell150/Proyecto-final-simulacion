# Mecánicas de física

Gravedad invertida, empuje de cajas y proyectiles parabólicos. **Toda la física se calcula por código**:
los `Rigidbody2D` quedan en modo *Kinematic* (Unity no les aplica gravedad ni fuerzas) y Unity solo se usa
para *detectar* contra qué choca cada objeto. Las velocidades, aceleraciones y la respuesta a los choques
salen de las fórmulas de abajo, integradas igual que en los talleres de la materia.

## Probarlo

1. Abrir `Assets/Scenes/PruebasFisica.unity` y darle Play.
2. Controles:

| Acción | Teclado | Mando |
|---|---|---|
| Moverse | A / D o flechas | Stick izquierdo o cruceta |
| Saltar | Espacio | A |
| Invertir gravedad | E | Y |
| Pausa | Esc | Start |

En la escena hay tres cajas (15 kg, 35 kg y 150 kg), un cañón que apunta al jugador cuando lo tiene a su
alcance, un hueco en el suelo y dos recargas de gravedad. La caja de 150 kg no se puede empujar: para pasarla
hay que invertir la gravedad y caminar por el techo, o subirse a la caja de 35 kg.

Si se cambia algo de los scripts del editor, la escena y los prefabs se pueden regenerar desde el menú
**Herramientas > Física > Crear escena de pruebas** (reemplaza la escena y los prefabs de `Assets/Prefabs/Fisica`).

## Unidades

Todo se calcula en el SI (m, s, kg, N) y se pasa a unidades de Unity solo al mover los objetos, como el
factor de escala del taller de movimiento parabólico:

- `PhysicsWorld.Gravity = 9.81` m/s²
- `PhysicsWorld.UnitsPerMeter = 1.5` → el caballero mide 2,4 unidades (1,6 m) y una caja normal 1,5 unidades (1 m).

## Integración (todos los cuerpos)

`PhysicsBody2D` aplica en cada `FixedUpdate` (Δt = 0,02 s) la segunda ley de Newton y el método de
Euler semi-implícito, el mismo orden de los talleres (primero la velocidad, luego la posición):

```
a = g + ΣF / m - (c / m)·v
v(t + Δt) = v(t) + a·Δt
x(t + Δt) = x(t) + v(t + Δt)·Δt
```

Para no atravesar paredes, el desplazamiento se hace primero en x y luego en y, lanzando la caja del collider
(`Physics2D.BoxCast`) en la dirección del movimiento. Si encuentra un obstáculo, el cuerpo avanza solo hasta
tocarlo y la componente de la velocidad contra él se resuelve con:

```
v' = v_obstáculo - e·(v - v_obstáculo)
```

con `e` el coeficiente de restitución (0 por defecto: no rebota). Si el obstáculo es una caja que se mueve,
el jugador queda a la velocidad de la caja.

## Gravedad invertida (`GravityInverter`)

- Al pulsar E / Y la gravedad del **jugador** cambia de sentido: `g = -9,81·ŷ` pasa a `g = +9,81·ŷ`.
  Las cajas y los proyectiles siguen con la gravedad normal.
- Tiene `maxUses` usos (3 por defecto). Por defecto solo se puede invertir estando apoyado (en el suelo o en
  el techo); con `allowInAir` se permite en el aire.
- Al invertirse, el salto también va en sentido contrario: `v0 = √(2·g·h)` hacia "arriba" del jugador.
- `RecargaGravedad` devuelve usos (sin pasar del máximo). Al reaparecer se restauran todos.
- El HUD (`GravityUsesHud`) muestra "Gravedad: usos/máximo" y parpadea si no se puede invertir.

## Empuje de cajas (`PushableBox`)

Rozamiento seco (modelo de Coulomb) con el suelo:

```
N = m·g
En reposo:   no se mueve mientras |F| ≤ μs·N
Deslizando:  f = -μk·N·signo(v)
a = (F + f) / m
```

La fuerza del jugador es como la de un motor: máxima con el jugador quieto y cero a su rapidez de caminar.

```
F = F_max·(1 - v_caja / v_caminar)        F_max = 250 N, v_caminar = 4 m/s
```

Por eso cada caja termina a una rapidez distinta, la que hace que F iguale al rozamiento cinético:

```
v = v_caminar·(1 - μk·m·g / F_max)
15 kg → 2,94 m/s      35 kg → 1,53 m/s      150 kg → no arranca (μs·m·g = 883 N > 250 N)
```

Al soltarla, el rozamiento cinético la frena con `a = -μk·g` y se detiene sin invertir el sentido.

## Proyectiles parabólicos (`ProjectileLauncher` y `Projectile`)

El modelo es el del taller de movimiento parabólico, con rozamiento lineal relativo al viento:

```
a = -g·ŷ - (c / m)·(v - v_viento)
```

Con `c = 0` (valor de la escena) es la parábola ideal:

```
x(t) = v0·cosθ·t          y(t) = v0·senθ·t - ½·g·t²
altura máxima h = v0y² / (2g)          alcance R = v0²·sen(2θ) / g
```

- El lanzador dispara cada `fireInterval` segundos con rapidez `launchSpeed` y ángulo `launchAngle`
  (grados desde +x; 135° es hacia arriba a la izquierda).
- Con `aimAtPlayer` calcula el ángulo para pasar por el jugador despejando θ de la trayectoria:
  `tanθ = (v² ± √(v⁴ - g·(g·x² + 2·y·v²))) / (g·x)` (signo − tiro bajo, + tiro alto con `useHighArc`).
  Si la raíz es negativa el jugador está fuera de alcance y usa el ángulo fijo.
- En el editor (vista Scene) se dibuja la trayectoria prevista en naranja y el punto más alto en amarillo.
- Choques: contra el jugador se comprueba a mano círculo contra caja (punto más cercano); si lo toca, el
  jugador reaparece. Contra lo sólido se usa un `CircleCast` entre la posición anterior y la nueva. Si golpea
  una caja le transmite velocidad con la fórmula del choque elástico del taller de colisiones:
  `v2' = (2·m1 / (m1 + m2))·v1 + ((m2 - m1) / (m1 + m2))·v2`.

## Morir y reaparecer

`PlayerController2D.Kill()` hace parpadear al jugador y lo devuelve al inicio (`SetSpawnPoint` permite
cambiarlo). Al reaparecer se dispara `PlayerController2D.Respawned`: las cajas vuelven a su sitio, se
borran los proyectiles y vuelven las recargas. `KillZone` (un `BoxCollider2D` marcado como trigger) mata al
jugador si su caja se solapa con la zona (`|x1 - x2| ≤ (a1 + a2)/2` en los dos ejes).

## Usar las mecánicas en un nivel

Arrastrar los prefabs de `Assets/Prefabs/Fisica`:

| Prefab | Para qué | Parámetros principales |
|---|---|---|
| `Jugador` | Personaje con física propia y gravedad invertible | Masa, velocidad, altura de salto, fuerza de empuje, usos de gravedad |
| `Caja` | Caja empujable | Masa (en `PhysicsBody2D`), μs y μk (en `PushableBox`) |
| `Lanzador` | Obstáculo que dispara proyectiles | v0, ángulo, intervalo, apuntar al jugador, masa, c y viento |
| `RecargaGravedad` | Devuelve usos de gravedad | Usos que devuelve |

Para el vacío o los pinchos: un objeto con `BoxCollider2D` y `KillZone`. El suelo, las paredes y el techo
solo necesitan un `BoxCollider2D` (sin `Rigidbody2D`). Los menús de pausa y opciones funcionan igual que
en los otros niveles (la escena de pruebas trae el prefab `PauseMenu`).

## Archivos

- `Assets/Scripts/Fisica/PhysicsWorld.cs`: constantes y fórmulas compartidas (solapamientos, choque elástico).
- `Assets/Scripts/Fisica/PhysicsBody2D.cs`: integración y choques de cualquier cuerpo.
- `Assets/Scripts/Fisica/GravityInverter.cs`, `GravityRecharge.cs`: gravedad invertida y sus recargas.
- `Assets/Scripts/Fisica/PushableBox.cs`: caja empujable con rozamiento.
- `Assets/Scripts/Fisica/Ballistics.cs`, `Projectile.cs`, `ProjectileLauncher.cs`: movimiento parabólico.
- `Assets/Scripts/Fisica/KillZone.cs`: zonas que matan.
- `Assets/Scripts/PlayerController2D.cs`: control del personaje (antes `Assets/pesonaje/movimiento personaje.cs`;
  se renombró para que el nombre del archivo coincida con el de la clase, si no Unity no puede usar el componente).
- `Assets/Scripts/GravityUsesHud.cs`: texto del HUD con los usos de gravedad.
- `Assets/Editor/PhysicsTestSceneBuilder.cs`: genera los prefabs y la escena de pruebas.
