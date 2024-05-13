from typing import TypeVar, Generic

T = TypeVar('T', int, float)

# Type alias: ion formulas are represended as a dictionary of element names and counts
IonFormula = dict[str, int]

class Vector3(Generic[T]):
    def __init__(self, x: T, y: T, z: T):
        self.x = x
        self.y = y
        self.z = z

    def __str__(self):
        return f"Vector3(x={self.x},y={self.y},z={self.z})"


class Extents:
    def __init__(self, min: Vector3[float], max: Vector3[float]):
        self.min = min
        self.max = max

    def __str__(self):
        return f"Extents(min={self.min},max={self.max})"

class IonInfo:
    def __init__(self, name: str, formula: IonFormula, volume: float, count: int):
        self.name = name
        self.formula = formula
        self.volume = volume
        self.count = count


class Range:
    def __init__(self, min: float, max: float):
        self.min = min
        self.max = max


class IonRanges:
    def __init__(self, name: str, formula: IonFormula, volume: float, ranges: list[Range]):
        self.name = name
        self.formula = formula
        self.volume = volume
        self.ranges = ranges