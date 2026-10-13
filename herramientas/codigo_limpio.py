"""Control de «código limpio» (material de la Etapa 3, Genially S12):
líneas de hasta 80 caracteres y funciones de hasta 15 líneas.
Además controla un error que sólo se ve en Windows: en .NET, asignar
MaximumSize con alto 0 achica el control a alto 0 (queda invisible);
para limitar sólo el ancho se usa Ui.Tope(ancho).

Uso: python3 herramientas/codigo_limpio.py [carpeta]
Cuenta las líneas no vacías del cuerpo de cada método, constructor o
descriptor de acceso (entre sus llaves, sin contarlas). Omite los archivos
*.generado.cs.
"""
import pathlib
import re
import sys

MAX_LINEA, MAX_FUNCION = 80, 15
FIRMA = re.compile(
    r'^\s*(public|private|protected|internal|static|override|virtual|'
    r'async|void|[A-Z][\w<>,\[\]\.? ]*)\b[^;=]*\)\s*(:\s*(base|this)\(.*\))?\s*$')
ACCESOR = re.compile(r'^\s*(get|set)\s*$')


def funciones(lineas):
    """Devuelve (nombre, línea, largo) de cada cuerpo de función."""
    res, i = [], 0
    while i < len(lineas) - 1:
        firma = lineas[i]
        es_func = FIRMA.match(firma) or ACCESOR.match(firma)
        es_func = es_func and not re.match(r'^\s*(if|for|foreach|while|'
                                           r'switch|using|catch|lock)\b',
                                           firma.strip())
        if es_func and lineas[i + 1].strip() == '{':
            prof, j, cuerpo = 0, i + 1, 0
            while j < len(lineas):
                prof += lineas[j].count('{') - lineas[j].count('}')
                if j > i + 1 and prof <= 0:
                    break
                if j > i + 1 and lineas[j].strip():
                    cuerpo += 1
                j += 1
            res.append((firma.strip()[:60], i + 1, cuerpo))
            i = j
        i += 1
    return res


ALTO_CERO = re.compile(r'MaximumSize\s*=\s*new\s+Size\([^()]*,\s*0\s*\)')


def alto_cero(texto):
    """Líneas donde se asigna MaximumSize con alto 0."""
    plano = re.sub(r'\s+', ' ', texto)
    return len(ALTO_CERO.findall(plano))


def main():
    raiz = pathlib.Path(sys.argv[1] if len(sys.argv) > 1 else '.')
    fallas = 0
    for f in sorted(raiz.rglob('*.cs')):
        if f.name.endswith('.generado.cs') or '/obj/' in str(f):
            continue
        lineas = f.read_text(encoding='utf-8').splitlines()
        for n, l in enumerate(lineas, 1):
            if len(l) > MAX_LINEA:
                fallas += 1
                print(f'{f}:{n}: línea de {len(l)} caracteres')
        for _ in range(alto_cero('\n'.join(lineas))):
            fallas += 1
            print(f'{f}: MaximumSize con alto 0 (usar Ui.Tope)')
        for nombre, n, largo in funciones(lineas):
            if largo > MAX_FUNCION:
                fallas += 1
                print(f'{f}:{n}: función de {largo} líneas: {nombre}')
    print(f'Código limpio: {fallas} observaciones')
    return 1 if fallas else 0


if __name__ == '__main__':
    sys.exit(main())
