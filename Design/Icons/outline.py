"""Removes the dark outline (stroke #13212e) from the FILLED shapes of an SVG fragment; strokes that ARE the drawing (fill="none": lines, rings) stay.
Used by the sprout badges and the shop glyphs, which no longer carry the navy outline of the older icons."""
import re

O = '#13212e'
_TAG = re.compile(r'<(?!/)([A-Za-z]+)([^>]*)>')
_STROKE_ATTRS = re.compile(r'\s(stroke|stroke-width|stroke-linejoin|stroke-linecap|paint-order)="[^"]*"')

def strip_outline(svg):
    def fix(m):
        attrs = m.group(2)
        if 'stroke="%s"' % O in attrs and 'fill="none"' not in attrs:
            attrs = _STROKE_ATTRS.sub('', attrs)
        return '<%s%s>' % (m.group(1), attrs)
    return _TAG.sub(fix, svg)
