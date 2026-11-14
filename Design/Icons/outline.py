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


CREAM = '#fff3d6'
_ATTR = re.compile(r'\b(fill|stroke)="%s"' % O)

def lighten_lines(svg):
    """For icons that stand directly on a dark background: text and stroke-only lines drawn in the dark ink (fill="none" + stroke) become cream so they stay visible.
    Dark filled shapes (eyes, dice pips...) keep their color, since they sit on light shapes of the same icon."""
    def fix(m):
        tag, attrs = m.group(1), m.group(2)
        if tag == 'text' and 'fill="%s"' % O in attrs:
            attrs = attrs.replace('fill="%s"' % O, 'fill="%s"' % CREAM)
        elif 'fill="none"' in attrs and 'stroke="%s"' % O in attrs:
            attrs = attrs.replace('stroke="%s"' % O, 'stroke="%s"' % CREAM)
        return '<%s%s>' % (tag, attrs)
    return _TAG.sub(fix, svg)
