"""Create original geometric artwork and public catalog. Requires Pillow only to regenerate assets."""
import json
from pathlib import Path
from PIL import Image, ImageDraw

root = Path(__file__).resolve().parent.parent
# The public catalog contains metadata only, never files or paths from a local installation.
rows = [
 ('classic','Angry Birds','2009','angrybirds','LA FIONDA','#F6C878','La fionda originale.'),
 ('go','Angry Birds Go!','2013','angrybirds','KART','#82D9E4','Una corsa tra vecchi amici.'),
 ('seasons','Angry Birds Seasons','2010','angrybirds','STAGIONI','#E7B3EC','Una festa, mille disastri.'),
 ('rio','Angry Birds Rio','2011','angrybirds','BRASILE','#F4C569','Si vola a Rio.'),
 ('space','Angry Birds Space','2012','angrybirds','GALASSIE','#A9B5FF','Caos a gravità zero.'),
 ('starwars','Angry Birds Star Wars','2012','angrybirds','LA SAGA','#91CFFF','Che la fionda sia con te.'),
 ('starwars2','Angry Birds Star Wars II','2013','angrybirds','LA SAGA','#FFB1A5','Scegli il tuo lato della Forza.'),
 ('piggies','Bad Piggies','2012','angrybirds','COSTRUZIONI','#ABD982','Costruisci. Sperimenta. Vola.'),
 ('pvz','Plants vs. Zombies','2009','pvz','GAME OF THE YEAR','#BADB87','Il tuo giardino ha bisogno di te.'),
 ('shellrazer','Shellrazer','2012','classics','LA TARTARUGA','#82D9E4','La tua fortezza in cammino.'),
]
catalog = []
for ident, name, year, group, label, accent, description in rows:
    owner = 'Rovio Entertainment'
    source = 'https://www.angrybirds.com/explore/hall-of-games/'
    availability = 'Titolo storico. Collega una copia già posseduta; la disponibilità dipende dal titolare e dalla piattaforma.'
    if ident == 'rio': owner += '; personaggi Rio: 20th Century Studios / Disney'
    if ident in ('starwars', 'starwars2'): owner += '; Star Wars: Lucasfilm / Disney'
    if ident == 'piggies':
        source = 'https://play.google.com/store/apps/details?id=com.rovio.BadPiggies'
        availability = 'Pagina ufficiale Android disponibile. La versione per PC può avere disponibilità diversa.'
    if ident == 'pvz':
        owner = 'PopCap Games / Electronic Arts'
        source = 'https://store.steampowered.com/app/3590/Plants_vs_Zombies_GOTY_Edition/'
        availability = 'Edizione GOTY per PC: consulta disponibilità e prezzo nello store ufficiale Steam.'
    if ident == 'shellrazer':
        owner = 'Slick Entertainment Inc.'
        source = 'https://play.google.com/store/apps/details?id=com.slickentertainment.Shellrazer'
        availability = 'Gioco Android storico. La pagina originale può non essere più disponibile. Collega una copia già posseduta tramite il tuo emulatore.'
    catalog.append(dict(id=ident, name=name, year=year, group=group, label=label, accent=accent,
        description=description, image='Assets/'+ident+'.png', owner=owner, sourceUrl=source, availability=availability))
    im = Image.new('RGBA', (256,256), '#161d2b')
    d = ImageDraw.Draw(im)
    d.rounded_rectangle((10,10,246,246), radius=54, outline=accent, width=5)
    d.ellipse((52,52,204,204), fill=accent)
    # All titles use the same neutral play glyph, no game logo or character art.
    d.polygon(((109,88),(109,168),(170,128)), fill='#162032')
    im.save(root / 'Assets' / (ident+'.png'))

for filename, color in [('hero.png','#e9ab63'), ('pvz-hero.png','#a9cf79'), ('classics-hero.png','#76c4ce')]:
    im=Image.new('RGB',(900,1000),'#172031'); d=ImageDraw.Draw(im)
    for y in range(1000): d.line((0,y,900,y),fill=(23+y//110,32-y//90,49-y//80))
    for x,y,r in [(160,170,110),(760,270,210),(450,580,270)]: d.ellipse((x-r,y-r,x+r,y+r),outline=color,width=2)
    d.rounded_rectangle((140,220,760,580),radius=95,fill=color)
    d.rounded_rectangle((160,240,740,560),radius=80,fill='#243148')
    d.rounded_rectangle((260,330,300,470),radius=8,fill=color)
    d.rounded_rectangle((210,380,350,420),radius=8,fill=color)
    d.ellipse((570,340,630,400),fill=color); d.ellipse((640,410,700,470),fill=color)
    im.save(root/'Assets'/filename)
(root/'catalog.json').write_text(json.dumps(catalog, ensure_ascii=False, indent=2),encoding='utf-8')
print('Generated original artwork and 10 public metadata entries')
