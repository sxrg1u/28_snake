using System;
using System.Collections.Generic;
using System.Text;

namespace _28_snake;

/// <summary>
/// Definiert die möglichen Bewegungsrichtungen der Schlange.
/// </summary>
public enum MovingDirection
{
    None,   // Schlange steht still (Spielstart)
    Up,     // Aufwärtsbewegung
    Down,   // Abwärtsbewegung
    Left,   // Linksbewegung
    Right   // Rechtsbewegung
}
