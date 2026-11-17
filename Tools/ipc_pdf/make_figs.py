import sys,json; sys.path.insert(0,__import__('os').path.dirname(__import__('os').path.abspath(__file__)))
from figures import *
top=['010','020','030','050','060','090','110','115','120','170','180','220','230']
steer=nodes_of('040')+['Collar','Main Swivel Arm']
retr=nodes_of('100')+nodes_of('160')
wheel=nodes_of('200')+['Steering Link Lower']
figs=[('Assembly', figure(top, exploded=False, out='sheet1.png')),
      ('Exploded view', figure(leaves, exploded=True, out='sheet2.png')),
      ('Detail A - steering', figure(['020','030','050','060','070','090'], focus=steer, out='sheet3.png')),
      ('Detail B - retraction and drag brace', figure(['110','115','120','130','140','150','170','180','190','210'], focus=retr, out='sheet4.png')),
      ('Detail C - strut and wheel', figure(['080','220','230'], focus=wheel, out='sheet5.png'))]
json.dump([[n,f] for n,f in figs],open('figs.json','w'),indent=1,default=float)
print([(n,len(f['labels'])) for n,f in figs])
