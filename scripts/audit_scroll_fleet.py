"""Static contract checks for scroll persistence and line-fleet navigation."""
from pathlib import Path
import re

ROOT = Path('Assets/Scripts')


def body(source, name):
    match = re.search(r'^    (?:static )?[\w<>\[\],]+ ' + re.escape(name) + r'\(', source, re.M)
    assert match, name
    start = source.index('{', match.end())
    depth = 1
    for i in range(start + 1, len(source)):
        depth += (source[i] == '{') - (source[i] == '}')
        if depth == 0:
            return source[start:i + 1]
    raise AssertionError(name)


def audit():
    screens = (ROOT / 'DemoScreens.cs').read_text(encoding='utf-8-sig')
    needed = {
        'DemoScreens.cs': ['OpenLinesScreen', 'BuildFleetDispatch', 'BuildFleetCollection', 'OpenLineFleet',
                           'BuildShopLiveries', 'BuildShopThemes', 'BuildCompanyOverviewTab', 'BuildCompanyBoardTab',
                           'BuildCompanyFinanceTab', 'BuildCompanyPrivilegeTab', 'OpenSceneryScreen', 'OpenRegionScreen'],
        'DemoCompanySystems.cs': ['BuildTalentManagementView'],
        'DemoJournalCulture.cs': ['BuildSceneryTicketsTab', 'BuildSceneryPlaquesTab', 'BuildScenerySolarTab'],
        'DemoStoryDirector.cs': ['OpenStoryArchiveModal']}
    fleet_calls = 0
    direct = []
    for path in ROOT.glob('*.cs'):
        source = path.read_text(encoding='utf-8-sig')
        assert 'OpenLineFleetSupply' not in source, path.name
        for line in source.splitlines():
            if 'FleetScroll(' in line and not 'RectTransform FleetScroll(' in line:
                assert 'scrollKey:' in line, (path.name, line)
                fleet_calls += 1
        headers = list(re.finditer(r'^    (?:static )?[\w<>\[\],]+ (\w+)\(', source, re.M))
        for scroll in re.finditer(r'AddComponent<ScrollRect>\(\)', source):
            name = [m[1] for m in headers if m.start() < scroll.start()][-1]
            if path.name == 'DemoResearch.cs':
                assert 'historyScrollY' in body(source, name)  # Tree already remembers its own 2D viewport.
            else:
                assert 'BindScrollPosition(' in body(source, name), (path.name, name)
                direct.append(path.name + ':' + name)
    for filename, names in needed.items():
        source = (ROOT / filename).read_text(encoding='utf-8-sig')
        for name in names:
            assert 'FleetScroll(' in body(source, name) and 'scrollKey:' in body(source, name), name
    restore = body(screens, 'RestoreScrollPosition')
    assert 'SavedScrollPosition(' in restore and 'scroll.StopMovement()' in restore and 'anchoredPosition =' in restore
    assert 'scroll.onValueChanged.AddListener' in body(screens, 'BindScrollPosition')
    assert 'IsCurrentScrollBinding(key, version)' in body(screens, 'BindScrollPosition')
    assert 'CaptureScrollPositions()' in body(screens, 'ClosePage')
    bootstrap = (ROOT / 'DemoBootstrap.cs').read_text(encoding='utf-8-sig')
    assert 'CaptureScrollPositions()' in body(bootstrap[bootstrap.index('    void ClosePanel(bool force)'):], 'ClosePanel')
    for field in ['fleetPowerFilter', 'fleetUseFilter', 'fleetRegionFilter']:
        assert f'if ({field} != value)' in body(screens, 'BuildFleetFilters')
    assert body(screens, 'BuildFleetFilters').count('ResetFleetFilterScrolls()') == 3
    assert 'if (sceneryCategory != value)' in body(screens, 'OpenSceneryScreen')
    assert 'if (sceneryRegionFilter != region)' in body(screens, 'OpenSceneryScreen')
    notice = body((ROOT / 'DemoBulletin.cs').read_text(encoding='utf-8-sig'), 'OpenNoticeBoard')
    assert 'if (scrollBound) RememberScrollPosition(' in notice and 'else RestoreScrollPosition(' in notice
    context = body(screens, 'ConfigureLineFleetContext')
    assert 'returnAction != null || lineFleetLineId != line.id || lineFleetBackAction == null' in context
    assert 'fleetBack = lineFleetOriginFleetBack' in context
    page = body(screens, 'OpenLineFleet')
    assert 'pageBack = ReturnFromLineFleet' in page and 'FleetGroupsByLine(line.id)' in page and 'FleetGroupsByLine(0)' in page
    assert 'lineFleetBackAction?.Invoke()' in body(screens, 'ReturnFromLineFleet')
    assert 'lineFleetSlot = -1' in body(screens, 'ReturnFromLineFleet')
    assert 'WithdrawVehicle' in page and 'AssignVehicle' in page
    assert 'line:fleet:' in page and 'LineFleetActions' in page
    assert 'shopCategory = 0;' in page and 'fleetBack = lineFleetShopReturn;' in page and 'OpenFleetTab(2)' in page
    assert 'lineFleetShopReturn = () => OpenLineFleet(line)' in page
    assert 'LocoPrice' not in page and 'PurchaseLoco' not in page and 'coins -=' not in page
    assert 'OpenLineFleet(line, () => OpenLineScreen(line.id))' in screens
    assert 'OpenLineFleet(line, () => OpenFleetTab(0))' in screens
    assert 'OpenLineFleet(line, () => OpenLineOperations(line), slot, amount)' in screens
    interactions = (ROOT / 'DemoLineInteractions.cs').read_text(encoding='utf-8-sig')
    assert 'OpenLineFleet(line, () => GoHome())' in interactions
    assert 'if (line.trains >= 8) { OpenLineFleet(line, () => GoHome()); return; }' in interactions
    assert 'MoveLineFleetVehicle(line, v, true)' in interactions
    move_error = body(screens, 'LineFleetMoveError')
    assert 'line.trains >= 8 || assigned >= 7' in move_error and 'PathTouchesRegion' in move_error
    assert 'SyncLineFleet(line)' in body(screens, 'MoveLineFleetVehicle')
    assert 'ApplyPendingLineFleetDispatch(line)' in body(screens, 'MoveLineFleetVehicle')
    assert 'lineFleetBackAction = null' in body(screens, 'GoHome')
    print('SCROLL_STATIC_PASS keyed_fleet_lists=' + str(fleet_calls) + ' keyed_scroll_rect_builders=' + str(len(direct)))
    print('LINE_FLEET_STATIC_PASS four_origins/grouped_inventory/no_embedded_purchases/shop_back/fixed_footer/capacity/region/dispatch')


if __name__ == '__main__':
    audit()
