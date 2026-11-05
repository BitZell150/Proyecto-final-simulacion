# Diseño de niveles — El Último Juramento

Rama de trabajo: `niveles`. Aquí va la lógica y las pruebas de los niveles; el arte se reemplaza después sin tocar la lógica.

## Mecánicas base
- Proyectil (flecha) con una sola trayectoria parabólica: sale horizontal, sin ángulo ni fuerza ajustables.
- Gravedad ajustable por el jugador desde el Nivel 1 (normal / invertida y magnitud). La flecha y la caja responden a esa gravedad.

## Niveles
| Escena | Nombre | Idea |
|---|---|---|
| Tutorial | Patio de entrenamiento | Movimiento, salto, caja, palanca, plataformas |
| Nivel1 | La fortaleza caída | Disparar con g invertida a una diana que saca un puente |
| Nivel2 | La cripta maldita | Nivel examen: pinchos móviles, caja por el techo, doble disparo |

## Reglas para evitar atajos
- Todo techo alcanzable con g invertida es letal o vacío, salvo zonas seguras marcadas.
- |g| entre 0,5·g₀ y 1,5·g₀ (nunca 0).
- Cambiar la gravedad y disparar solo con el personaje en el suelo.
- Al cambiar la gravedad, la velocidad horizontal se pone a 0.
- Morir reinicia el nivel completo (puentes, dianas y caja incluidos).
