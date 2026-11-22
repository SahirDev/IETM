"""Writes Assets/IETM/Resources/IETM/engine_ietm.json: the F-16 engine (turbine module) IETM, same structure as the
landing gear publication. Node names are those of Assets/Model/engine.glb (turbine_01_obj children).
Offsets (explode / removal / removalVia) are in the glTF space of turbine_01_obj: +X = towards the exhaust (aft),
-Z = up, +Z = down, +Y = to the side (away from the default view). SAMPLE DATA."""
import json

N = {  # IPD item -> glTF node
    '010': 'hull_turbine_004', '020': 'hull_turbine', '030': 'mount_turbine_014', '040': 'pipes_turbine_009',
    '050': 'screws_turbine_001', '110': 'blades_turbine_003', '120': 'blades_dividers_turbine_017',
    '130': 'plates_turbine_016', '210': 'canister_turbine_011', '220': 'grid_turbine_007', '230': 'blades_turbine_003.001',
}

def item(no, desc, pn, indent, upa, smr, info, remarks=None, nodes=None):
    d = {'itemNo': no, 'isn': '00A', 'cage': 'K0F16', 'partNumber': pn, 'description': desc, 'indent': indent, 'upa': upa,
         'smr': smr, 'nodes': nodes if nodes is not None else ([N[no]] if no in N else []), 'info': info}
    if remarks: d['remarks'] = remarks
    return d

items = [
    item('001', 'ENGINE ASSY - TURBOFAN, AFTERBURNING', 'ENG-72-0001-01', 1, 'RF', 'PAOOO',
         'Complete engine (turbine module) as installed in the aircraft: compressor, combustion and turbine sections in the outer case, with the external lines, mounts and the exhaust cone.'),
    item('010', 'CASE - OUTER, ENGINE', 'ENG-72-1010-01', 2, '1', 'PAOGG',
         'Outer engine case. Carries the bypass air and holds the compressor, combustor and turbine in line.', 'Inspect the case flanges for cracks and fretting.'),
    item('020', 'CONE - EXHAUST, TAIL', 'ENG-72-1020-02', 2, '1', 'PAOZZ',
         'Exhaust (tail) cone at the turbine exit. Shapes the hot gas flow into the exhaust nozzle.'),
    item('030', 'MOUNT - ENGINE SUPPORT RING', 'ENG-71-2030-01', 2, '1', 'PAOZZ',
         'Support ring and brackets that carry the engine on the aircraft mounts and take the thrust load.', 'Check the mount pads and bolt holes for elongation.'),
    item('040', 'TUBE ASSY - FUEL AND OIL, EXTERNAL', 'ENG-73-3040-01', 2, '1', 'PAOZZ',
         'External fuel, oil and sense lines routed around the case to the fuel nozzles, bearings and sensors.', 'Cap all openings when the lines are disconnected.'),
    item('050', 'BOLT SET - CASE FLANGE', 'ENG-72-1050-01', 2, 'AR', 'PAOZZ',
         'Bolts and nuts that join the case flanges and hold the support ring.', 'Discard self-locking nuts after removal.'),
    item('100', 'COMPRESSOR SECTION', 'ENG-72-3100-00', 2, 'REF', 'XBOOO',
         'Front of the engine: the fan, the inlet guide vanes and the compressor stages raise the air pressure before combustion.', nodes=[]),
    item('110', 'ROTOR - FAN, BLADED', 'ENG-72-3110-01', 3, '1', 'PAOGG',
         'Fan rotor: the first rotating blades at the engine inlet. Draws air in and sends part of it around the core as bypass air.', 'Inspect the leading edges for foreign object damage (FOD).'),
    item('120', 'VANE ASSY - INLET GUIDE', 'ENG-72-3120-01', 3, '1', 'PAOZZ',
         'Inlet guide vanes (stator): fixed vanes that turn the air onto the fan and compressor blades at the right angle.'),
    item('130', 'DISK ASSY - COMPRESSOR STAGES', 'ENG-72-3130-01', 3, '1', 'PAOGG',
         'Compressor disk stack: the stages that compress the core air before it enters the combustor.'),
    item('200', 'HOT SECTION', 'ENG-72-4200-00', 2, 'REF', 'XBOOO',
         'Combustion and turbine: fuel is burned in the compressed air and the hot gas drives the turbine.', nodes=[]),
    item('210', 'CASE - COMBUSTOR', 'ENG-72-4210-01', 3, '1', 'PAOGG',
         'Combustor case: pressure vessel around the combustion chamber that carries the fuel nozzles.', 'Inspect for hot spots, burn-through and distortion.'),
    item('220', 'LINER - COMBUSTOR, PERFORATED', 'ENG-72-4220-01', 3, '1', 'PAOZZ',
         'Perforated combustor liner: holds the flame and lets cooling air in through its holes.', 'Borescope: look for cracks between the cooling holes.'),
    item('230', 'ROTOR - TURBINE, BLADED', 'ENG-72-4230-01', 3, '1', 'PAOGG',
         'Turbine rotor stages: take energy from the hot gas to drive the compressor and the fan through the shaft.', 'Borescope: look for blade tip erosion, coating loss and nicks.'),
]
by = {i['itemNo']: i for i in items}

def detail(no):
    i = by[no]
    return '• Item %s · %s · P/N %s - %s' % (no.lstrip('0') or '0', i['description'].title().replace(' - ', ' - '), i['partNumber'], i['info'])

# disassembly order: outside in, aft parts aft, front parts forward. (title, item, how, removal end, removal via)
DIS = [
    ('Remove the case flange bolts', '050', 'Remove the nuts and bolts from the case flanges and the support ring. Keep the bolt sets together.', (-3.0, 0, -2.2), (-3.0, 0, 0)),
    ('Disconnect and remove the external lines', '040', 'Disconnect the fuel, oil and sense lines at their unions. Cap all openings, then take the tube assembly off.', (-3.0, 0, 2.2), (-3.0, 0, 0)),
    ('Remove the engine support ring', '030', 'Support the engine on the stand. Slide the support ring aft off the case and set it down.', (4.2, 0, -2.2), (4.2, 0, 0)),
    ('Remove the outer case', '010', 'Slide the outer case aft off the core and set it on the case stand.', (4.6, 2.3, 0), (4.6, 0, 0)),
    ('Remove the exhaust cone', '020', 'Remove the exhaust cone from the turbine exit.', (4.6, 0, 0), None),
    ('Remove the turbine rotor', '230', 'Pull the turbine rotor stages aft off the shaft and put them on the rotor stand.', (2.3, 0, 2.2), (2.3, 0, 0)),
    ('Remove the combustor case', '210', 'Slide the combustor case aft off the liner.', (3.9, 0, 0), None),
    ('Remove the combustor liner', '220', 'Slide the perforated liner aft out of the combustion section.', (1.6, 0, 0), None),
    ('Remove the fan rotor', '110', 'Remove the fan rotor from the front of the shaft.', (-1.4, 0, 0), None),
    ('Remove the inlet guide vanes', '120', 'Remove the inlet guide vane assembly from the front frame.', (-2.7, 0, 0), None),
    ('Remove the compressor disk assembly', '130', 'Pull the compressor disk stack forward off the shaft.', (-0.9, 0, 0), None),
]

removal = [{'node': N[no], 'offset': list(end)} for _, no, _, end, _ in DIS]
removal_via = [{'node': N[no], 'offset': list(via)} for _, no, _, _, via in DIS if via]
# exploded view (slider): a gentle spread along the engine axis and out from the core
explode = [
    {'node': N['050'], 'offset': [0, 0, -0.55]}, {'node': N['040'], 'offset': [0, 0, 0.6]}, {'node': N['030'], 'offset': [0.4, 0.75, 0]},
    {'node': N['010'], 'offset': [0, 0, -1.25]}, {'node': N['020'], 'offset': [1.6, 0, 0]}, {'node': N['230'], 'offset': [1.0, 0, 0]},
    {'node': N['210'], 'offset': [0, 0, 1.45]}, {'node': N['220'], 'offset': [0.3, 0, 0]}, {'node': N['110'], 'offset': [-0.9, 0, 0]},
    {'node': N['120'], 'offset': [-1.7, 0, 0]}, {'node': N['130'], 'offset': [-0.45, 0, 0]},
]

dis_steps = [{'title': 'Preliminary requirements',
              'text': 'Engine removed from the aircraft and installed on the engine maintenance stand. Fuel and oil drained. The removed parts are laid out around the engine in the order they come off - nothing overlaps.',
              'warning': 'WARNING: Let the engine cool down before you start. Hot section parts can cause burns.',
              'focus': ['001'], 'action': 'highlight', 'view': 'iso'}]
for k, (title, no, how, _, _) in enumerate(DIS):
    dis_steps.append({'title': '%d. %s' % (k + 1, title), 'text': how + '\n\nPart details:\n' + detail(no), 'focus': [no], 'action': 'detach'})
dis_steps.append({'title': 'Disassembly complete', 'text': 'All parts are off the engine and laid out. Clean and examine each part before you assemble the engine again.',
                  'focus': ['001'], 'action': 'highlight'})

asm_steps = [{'title': 'Preliminary requirements',
              'text': 'All parts cleaned, examined and serviceable, laid out around the stand. Assembly is the disassembly in reverse order.',
              'caution': 'CAUTION: Use new self-locking nuts and seals. Torque all flange bolts to the values in the torque table.',
              'focus': [no for _, no, _, _, _ in DIS], 'action': 'detach', 'view': 'iso'}]
for k, (title, no, how, _, _) in enumerate(reversed(DIS)):
    name = title.replace('Remove the ', '').replace('Disconnect and remove the ', '')
    asm_steps.append({'title': '%d. Install the %s' % (k + 1, name), 'text': 'Put the %s back in position and secure it.\n\nPart details:\n%s' % (name, detail(no)),
                      'focus': [no], 'action': 'install'})
asm_steps.append({'title': 'Assembly complete', 'text': 'The engine is assembled. Do the borescope inspection and an engine ground run before flight.',
                  'focus': ['001'], 'action': 'highlight'})

ROTORS = ['110', '130', '230']
oper_steps = [
    {'title': 'Operation - overview', 'text': 'Air enters at the front, is compressed, mixed with fuel and burned, and the hot gas drives the turbine before it leaves through the exhaust. The rotors turn on the shaft while this module is shown. Use Next to follow the air through the engine; the parts that do the work stay in colour.',
     'focus': ['001'], 'action': 'operate', 'animate': 'spin', 'spin': ROTORS, 'view': 'iso'},
    {'title': '1. Air enters the inlet', 'text': 'The inlet guide vanes (120) turn the incoming air so that it meets the fan blades at the right angle.',
     'focus': ['120'], 'action': 'operate', 'animate': 'spin', 'spin': ROTORS},
    {'title': '2. The fan draws the air in', 'text': 'The fan rotor (110) draws air into the engine. Part of it goes around the core as bypass air; the rest goes on to the compressor.',
     'focus': ['110'], 'action': 'operate', 'animate': 'spin', 'spin': ROTORS},
    {'title': '3. The compressor raises the pressure', 'text': 'The compressor stages (130) squeeze the core air to many times the inlet pressure before it reaches the combustor.',
     'focus': ['130'], 'action': 'operate', 'animate': 'spin', 'spin': ROTORS},
    {'title': '4. Fuel is burned in the combustor', 'text': 'The external lines (40) bring fuel to the nozzles in the combustor case (210). The fuel burns in the compressed air inside the perforated liner (220); cooling air flows in through its holes.',
     'focus': ['210', '220', '040'], 'action': 'operate', 'animate': 'spin', 'spin': ROTORS},
    {'title': '5. The hot gas drives the turbine', 'text': 'The hot gas expands through the turbine rotor (230). The turbine turns the shaft that drives the compressor and the fan.',
     'focus': ['230'], 'action': 'operate', 'animate': 'spin', 'spin': ROTORS},
    {'title': '6. Exhaust and thrust', 'text': 'The gas leaves past the exhaust cone (20) into the nozzle and produces thrust. The outer case (10) and the support ring (30) carry the thrust into the aircraft structure.',
     'focus': ['020', '010', '030'], 'action': 'operate', 'animate': 'spin', 'spin': ROTORS},
]

bsi_steps = [
    {'title': 'Preparation', 'text': 'Engine cold and on the stand. Open the borescope ports and connect the borescope.',
     'warning': 'WARNING: Make sure that the ignition is disarmed and the engine cannot be motored while the borescope is inserted.',
     'focus': ['001'], 'action': 'highlight', 'view': 'iso'},
    {'title': 'Fan and inlet guide vanes', 'text': 'Examine the fan blades (110) and the inlet guide vanes (120) for nicks, dents and foreign object damage.',
     'focus': ['110', '120'], 'action': 'isolate', 'view': 'front'},
    {'title': 'Compressor stages', 'text': 'Turn the rotor slowly by hand and examine each compressor stage (130) for blade damage and rubbing.',
     'focus': ['130'], 'action': 'isolate', 'view': 'iso'},
    {'title': 'Combustor liner', 'text': 'Examine the perforated liner (220) for cracks between the cooling holes, burn-through and distortion.',
     'focus': ['220', '210'], 'action': 'isolate', 'view': 'side'},
    {'title': 'Turbine blades', 'text': 'Examine the turbine blades (230) for tip erosion, coating loss and cracks. Record the findings with images.',
     'focus': ['230'], 'action': 'isolate', 'view': 'iso'},
    {'title': 'Close-up', 'text': 'Remove the borescope, install the port plugs and record the inspection in the engine log.',
     'focus': ['001'], 'action': 'restore', 'view': 'iso'},
]

G0, G1, G2, G3 = '00 Introduction', '72-00 Description and operation', '72-00 Maintenance', '72-00 Illustrated parts data'
modules = [
    {'id': 'DM-ENG-TITLE', 'group': G0, 'type': 'description', 'dmc': 'DMC-F16ENG-A-00-00-00-00A-001A-D', 'title': 'Title page', 'infoName': 'Title page',
     'paragraphs': [
         {'heading': 'F-16 Engine - Turbine Module', 'text': 'Interactive Electronic Technical Manual (IETM) for the F-16 engine (turbine module). The manual combines descriptive data, an interactive Illustrated Parts Data (IPD) module and step-by-step procedures, all linked to the 3D model.'},
         {'heading': 'How to use', 'text': 'Select a data module in the publication tree. Click a part in the 3D view or a row in the parts list to cross-highlight it. Use the viewer toolbar to isolate, hide, x-ray or explode the engine. Add parts to the order list with the cart button.'},
         {'heading': 'Safety', 'text': 'Engine work is done with the engine on the maintenance stand, cold, drained and with the ignition disarmed. Follow the warnings and cautions in each procedure.'},
         {'heading': 'Data status', 'text': 'This is a proof-of-concept publication. Part numbers, CAGE codes and procedures are sample data.'}]},
    {'id': 'DM-ENG-DESC', 'group': G1, 'type': 'description', 'dmc': 'DMC-F16ENG-A-72-00-00-00A-040A-D', 'title': 'Engine', 'infoName': 'Description', 'focus': ['001'],
     'paragraphs': [
         {'heading': 'General', 'text': 'The F-16 is powered by one afterburning turbofan engine. Air flows from the inlet at the front through the compressor, the combustor and the turbine to the exhaust at the rear. Select a heading to isolate that part of the engine in the 3D view: it keeps its own colours and the rest of the engine turns transparent.', 'focus': ['001']},
         {'heading': 'Compressor section', 'text': 'The inlet guide vanes (120) turn the air onto the fan rotor (110). The compressor stages (130) raise the pressure of the core air before combustion.', 'focus': ['100']},
         {'heading': 'Combustion', 'text': 'The combustor case (210) holds the perforated liner (220), where fuel from the external lines (40) is burned in the compressed air.', 'focus': ['210', '220', '040']},
         {'heading': 'Turbine and exhaust', 'text': 'The turbine rotor (230) takes energy from the hot gas to drive the compressor and the fan. The exhaust cone (20) guides the gas into the nozzle.', 'focus': ['230', '020']},
         {'heading': 'Case and mounts', 'text': 'The outer case (10) holds the sections in line and is joined at its flanges by the bolt set (50). The support ring (30) carries the engine and its thrust into the aircraft structure.', 'focus': ['010', '030', '050']}]},
    {'id': 'DM-ENG-OPER', 'group': G1, 'type': 'procedure', 'dmc': 'DMC-F16ENG-A-72-00-00-00A-041A-D', 'title': 'Engine', 'infoName': 'Operation (gas path)', 'steps': oper_steps},
    {'id': 'DM-ENG-DISASSY', 'group': G2, 'type': 'procedure', 'dmc': 'DMC-F16ENG-A-72-00-00-00A-520A-A', 'title': 'Engine', 'infoName': 'Disassembly', 'steps': dis_steps},
    {'id': 'DM-ENG-ASSY', 'group': G2, 'type': 'procedure', 'dmc': 'DMC-F16ENG-A-72-00-00-00A-720A-A', 'title': 'Engine', 'infoName': 'Assembly', 'steps': asm_steps},
    {'id': 'DM-ENG-BSI', 'group': G2, 'type': 'procedure', 'dmc': 'DMC-F16ENG-A-72-00-00-00A-310A-A', 'title': 'Engine', 'infoName': 'Borescope inspection', 'steps': bsi_steps},
    {'id': 'DM-ENG-IPD', 'group': G3, 'type': 'ipd', 'dmc': 'DMC-F16ENG-A-72-00-00-00A-941A-D', 'title': 'Engine', 'infoName': 'Illustrated parts data - IPD'},
]

data = {
    'publication': {'title': 'F-16 Engine - Turbine Module', 'subtitle': 'Interactive Electronic Technical Manual', 'pmc': 'PMC-F16ENG-K0F16-00001-00',
                    'issue': '001', 'issueDate': '2026-11-22', 'applicability': 'F-16 engine turbine module scale model - Serial 001',
                    'dataStatus': 'SAMPLE DATA - part numbers, CAGE codes and procedures are placeholders for the POC. Replace with OEM data.'},
    'figure': {'title': 'Engine - Turbine Module', 'icn': 'ICN-F16ENG-A-720000-0-K0F16-00001-A-01-1', 'figureNumber': 'Fig. 1'},
    'items': items, 'modules': modules, 'explode': explode, 'removal': removal, 'removalLPath': False, 'removalVia': removal_via,
}
import os
out = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'Assets/IETM/Resources/IETM/engine_ietm.json')
json.dump(data, open(out, 'w', encoding='utf-8'), indent=1, ensure_ascii=False)
print('wrote', out, len(items), 'items,', len(modules), 'modules,', len(dis_steps), 'disassembly steps')
