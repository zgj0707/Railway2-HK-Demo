"""Audit the shipping company DAG, including all possible first-six unlock states.

Run from the project root: python scripts/audit_company_dag.py
"""
from pathlib import Path
import collections
import json
import re

SOURCE = Path('Assets/Scripts/DemoCompanySystems.cs')
MILESTONES = ['co_stock_1821', 'co_shares_1825', 'co_fin_1', 'co_ops_1',
              'co_rch_1842', 'co_fin_2', 'co_ops_2', 'co_fin_3', 'co_audit_1885']


def load_nodes():
    nodes = {}
    for block in re.findall(r'new CompanyTechNode \{.*?\}(?=,?\n)', SOURCE.read_text(encoding='utf-8-sig'), re.S):
        def text(field):
            hit = re.search(field + r' = "([^"]*)"', block)
            return hit[1] if hit else None
        parent_match = re.search(r'parents = new\[\] \{ ([^}]+) \}', block)
        node = dict(key=text('key'), title=text('title'), branch=int(re.search(r'branch = (\d+)', block)[1]),
                    year=int(re.search(r'year = (\d+)', block)[1]),
                    parents=re.findall(r'"([^"]+)"', parent_match[1]) if parent_match else [],
                    resolvedBy=text('resolvedBy'))
        assert text('periodLabel').startswith(str(node['year'])), ('periodLabel year', node['key'])
        assert node['key'] not in nodes
        nodes[node['key']] = node
    return nodes


def audit(nodes):
    root = 'co_charter_1801'
    assert len(nodes) == 105
    assert [k for k, n in nodes.items() if not n['parents']] == [root]
    children = collections.defaultdict(list)
    cross = edges = 0
    for key, node in nodes.items():
        assert len(set(node['parents'])) == len(node['parents']), key
        for parent in node['parents']:
            assert parent in nodes and nodes[parent]['year'] <= node['year'], (key, parent)
            children[parent].append(key)
            edges += 1
            cross += nodes[parent]['branch'] != node['branch']
        if node['resolvedBy']:
            assert key in nodes[node['resolvedBy']]['parents'], ('lesson remedy', key)
    assert {nodes[k]['title'] for k in children[root]} == {'雇员名册', '站前货栈', '汇票结算'}
    assert len(children[root]) == 3 and max(map(len, children.values())) <= 4
    assert cross / edges >= .35
    merges = sum(len(n['parents']) >= 2 for n in nodes.values())
    assert merges >= 35

    def ready(done):
        return {k for k, n in nodes.items() if k not in done and set(n['parents']) <= done}

    def visible(done):
        return {k for k, n in nodes.items() if not n['parents'] or any(p in done for p in n['parents'])}

    assert ready(set()) == {root}
    assert len(ready({root})) == 3
    assert all(nodes[k]['branch'] < 3 for k in visible({root}))
    states = {frozenset()}
    early = []
    for step in range(7):
        width = max(len(ready(done)) for done in states)
        assert width <= 5, (step, width)
        early.append(dict(unlocked=step, distinctStates=len(states), maxReady=width))
        states = {done | {key} for done in states for key in ready(done)}

    depth = {}
    def get_depth(key, visiting=None):
        if key in depth:
            return depth[key]
        visiting = set() if visiting is None else visiting
        assert key not in visiting, ('cycle', key)
        visiting.add(key)
        depth[key] = max((get_depth(p, visiting) + 1 for p in nodes[key]['parents']), default=0)
        visiting.remove(key)
        return depth[key]
    for key in nodes:
        get_depth(key)

    def coins(key):
        if key == root:
            return 0
        node = nodes[key]
        raw = 200 * 5000 ** ((node['year'] - 1800) / 99) * (.65 if node['branch'] == 4 else 1)
        step = 1000 if raw >= 10000 else 100 if raw >= 1000 else 10
        return max(100, min(1000000, round(raw / step) * step))

    simulations = {}
    for strategy in ['cost', 'depth']:
        done = set()
        events = []
        for step in range(105):
            options = ready(done)
            assert options, ('dead end', strategy, step)
            if step <= 6:
                assert len(options) <= 5
            key = min(options, key=lambda k: ((coins(k) if strategy == 'cost' else depth[k]), coins(k), k))
            old_visible, old_ready = visible(done), ready(done)
            done.add(key)
            if key in MILESTONES:
                new_visible = visible(done) - old_visible
                new_ready = ready(done) - old_ready
                assert 2 <= len(new_visible) <= 4, (strategy, key, new_visible)
                events.append(dict(key=key, newVisible=len(new_visible), newReady=len(new_ready)))
        assert done == nodes.keys()
        simulations[strategy] = events
    rings = collections.Counter(0 if n['year'] < 1810 else 1 if n['year'] < 1820 else 2 if n['year'] < 1830
                                else 3 if n['year'] < 1850 else 4 if n['year'] < 1870 else 5 for n in nodes.values())
    return dict(nodes=len(nodes), rootFanout=3, maxFanout=max(map(len, children.values())), edges=edges,
                crossBranchEdges=cross, crossBranchRatio=cross/edges, mergeNodes=merges,
                earlyExhaustiveStates=early, milestones=simulations, rings=dict(sorted(rings.items())))


if __name__ == '__main__':
    print(json.dumps(audit(load_nodes()), ensure_ascii=False, indent=2))
