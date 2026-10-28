"""python3 check_layout.py layout_2_1 [--map]: checks a layout can be finished and prints it"""
import sys, importlib, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import levelkit
for name in [a for a in sys.argv[1:] if not a.startswith('--')]:
    mod = importlib.import_module(name)
    ok, seen = levelkit.check(mod)
    print(name, 'OK' if ok else 'PROBLEMS')
    if '--map' in sys.argv:
        print(levelkit.ascii(mod, seen))
