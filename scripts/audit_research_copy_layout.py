"""Check exported shipping copy against the game's TTF metrics at all font tiers.

This is a conservative font-metric audit, not a Unity TextGenerator render test.
The JSON is exported by ResearchCopyAudit from the compiled game copy builders.
"""
from pathlib import Path
from functools import lru_cache
import argparse
import json
import math
from PIL import ImageFont
from fontTools.ttLib import TTFont

FONT = Path('Assets/Resources/Fonts/LXGWWenKai-Regular.ttf')
SCALES = (.9, 1., 1.16, 1.32)


@lru_cache(None)
def font(size):
    return ImageFont.truetype(str(FONT), size)


@lru_cache(None)
def advance(char, size):
    return font(size).getlength(char)


def rows(text, size, width):
    count = 0
    for paragraph in text.split('\n'):
        current = 0
        count += 1
        for char in paragraph:
            w = advance(char, size)
            if current and current + w > width:
                count += 1
                current = 0
            current += w
    return count


def preferred(text, size, width, spacing=1.):
    return math.ceil(rows(text, size, width) * sum(font(size).getmetrics()) * spacing)


def scaled(size, scale, cap=1.32):
    return max(16, round(size * min(scale, cap)))


def audit(cases):
    cmap = TTFont(FONT).getBestCmap()
    characters = set(''.join(''.join(r['fields'].values()) + r['badge'] for r in cases))
    missing = [c for c in characters if not c.isspace() and ord(c) not in cmap]
    assert not missing, ('font glyph coverage', missing)
    counts = {'engineering': len({r['key'] for r in cases if not r['corporate']}),
              'company': len({r['key'] for r in cases if r['corporate']}),
              'copyStates': len(cases), 'fontTiers': len(SCALES), 'missingGlyphs': len(missing)}
    heights, badge_sizes, body_sizes, details = [], [], [], []
    for case in cases:
        for scale in SCALES:
            fields = case['fields']
            for name, text in fields.items():
                assert text == text.strip() and '\n\n' not in text and '\r' not in text, (case['key'], name)
                for line in text.split('\n'):
                    assert line == line.strip() and not line.startswith('·') and not line.endswith('·'), (case['key'], name)
            eyebrow_size = scaled(24, scale)
            assert '\n' not in fields['eyebrow']
            assert font(eyebrow_size).getlength(fields['eyebrow']) <= 768, ('eyebrow width', case['key'], scale)
            eyebrow = preferred(fields['eyebrow'], eyebrow_size, 768) + 4
            title_rows = rows(fields['title'], scaled(38, scale), 768)
            title = preferred(fields['title'], scaled(38, scale), 768) + 4
            footer = preferred(fields['status'], scaled(25, scale), 736) + 24
            size = scaled(28, scale)
            body = preferred(fields['body'], size, 768, 1.22) + 6
            while eyebrow + title + body + footer + 168 > 760 and size > 24:
                size -= 1
                body = preferred(fields['body'], size, 768, 1.22) + 6
            height = max(310, eyebrow + title + body + footer + 168)
            assert height <= 760, ('height', case['key'], case['state'], scale, height)
            assert size >= 24, ('readable body', case['key'], scale)
            body_sizes.append(size)
            # Card badge must fit in ONE line at or above the existing 15px floor.
            badge_size = None
            for n in range(scaled(20, scale, 1.24), 14, -1):
                if font(n).getlength(case['badge']) <= 188 and sum(font(n).getmetrics()) <= 34:
                    badge_size = n
                    break
            assert badge_size is not None, ('card badge', case['key'], case['state'], case['badge'])
            card_title_height = 32 if case['key'].startswith('loco') else 48
            card_title_size = next((n for n in range(scaled(25 if case['state'] == 'ready' else 23, scale, 1.24), 15, -1)
                                    if preferred(fields['title'], n, 188) <= card_title_height), None)
            assert card_title_size is not None, ('card title', case['key'], scale)
            badge_sizes.append(badge_size)
            heights.append(height)
            details.append(dict(key=case['key'], corporate=case['corporate'], state=case['state'], scale=scale,
                                eyebrowRows=1, titleRows=title_rows, bodyRows=rows(fields['body'], size, 768),
                                statusRows=rows(fields['status'], scaled(25, scale), 736),
                                height=height, bodyFont=size, badgeFont=badge_size, cardTitleFont=card_title_size))
    counts.update(layoutCases=len(details), minHeight=min(heights), maxHeight=max(heights),
                  minBadgeFont=min(badge_sizes), minBodyFont=min(body_sizes), maxEyebrowRows=1,
                  maxBodyRows=max(r['bodyRows'] for r in details), maxStatusRows=max(r['statusRows'] for r in details),
                  minCardTitleFont=min(r['cardTitleFont'] for r in details),
                  measurement='Pillow/FreeType metrics of project TTF; not native Unity layout')
    return counts, details


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('cases', type=Path)
    parser.add_argument('--report', type=Path)
    args = parser.parse_args()
    summary, details = audit(json.loads(args.cases.read_text(encoding='utf-8')))
    if args.report:
        args.report.write_text(json.dumps(dict(summary=summary, layouts=details), ensure_ascii=False, indent=2), encoding='utf-8')
    print(json.dumps(summary, ensure_ascii=False, indent=2))
