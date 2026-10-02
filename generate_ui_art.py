import math
from PIL import Image, ImageDraw, ImageFilter

def create_dash_ring():
    size = 128
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # Outer dark shadow
    draw.ellipse([4, 4, size - 5, size - 5], fill=(15, 10, 8, 220))
    # Outer gold border
    draw.ellipse([6, 6, size - 7, size - 7], outline=(225, 185, 85, 255), width=4)
    # Inner gold border highlight
    draw.ellipse([8, 8, size - 9, size - 9], outline=(255, 230, 140, 180), width=1)
    # Dark inner circular backing
    draw.ellipse([10, 10, size - 11, size - 11], fill=(22, 26, 32, 240))
    # Inner subtle rim
    draw.ellipse([12, 12, size - 13, size - 13], outline=(40, 50, 65, 200), width=2)
    
    img.save('Assets/Image/UI/Dash_Ring.png')
    print("Dash_Ring.png created")

def create_dash_mask():
    size = 128
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    # Solid white disc matching inner area of ring
    draw.ellipse([10, 10, size - 11, size - 11], fill=(255, 255, 255, 255))
    img.save('Assets/Image/UI/Dash_Cooldown_Mask.png')
    print("Dash_Cooldown_Mask.png created")

def create_dash_icon():
    size = 128
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # Draw swift dash arrow-wings (3 diagonal speed streaks)
    # Streak 1 (center main arrow)
    pts1 = [(32, 78), (70, 40), (84, 40), (46, 78)]
    draw.polygon(pts1, fill=(80, 235, 235, 255))
    
    # Arrow head
    head = [(60, 48), (94, 34), (80, 68), (72, 54)]
    draw.polygon(head, fill=(130, 255, 255, 255))
    
    # Streak 2 (upper trail)
    pts2 = [(44, 52), (68, 28), (80, 28), (56, 52)]
    draw.polygon(pts2, fill=(60, 200, 220, 220))
    
    # Streak 3 (lower trail)
    pts3 = [(30, 94), (54, 70), (66, 70), (42, 94)]
    draw.polygon(pts3, fill=(40, 160, 200, 200))
    
    # Front tip glow
    draw.ellipse([88, 30, 98, 40], fill=(255, 255, 255, 255))
    
    img.save('Assets/Image/UI/Dash_Icon.png')
    print("Dash_Icon.png created")

def create_quest_banner():
    w, h = 360, 110
    img = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # Shadow
    draw.rounded_rectangle([3, 3, w - 4, h - 4], radius=10, fill=(10, 8, 7, 180))
    # Dark fantasy wood / charcoal panel
    draw.rounded_rectangle([5, 5, w - 6, h - 6], radius=9, fill=(20, 18, 22, 235))
    
    # Golden border
    draw.rounded_rectangle([5, 5, w - 6, h - 6], radius=9, outline=(195, 155, 75, 240), width=2)
    # Inner delicate border
    draw.rounded_rectangle([9, 9, w - 10, h - 10], radius=6, outline=(100, 80, 45, 150), width=1)
    
    # Left decorative quest ribbon tab
    draw.rectangle([5, 12, 14, h - 13], fill=(215, 170, 65, 255))
    draw.rectangle([14, 12, 16, h - 13], fill=(255, 225, 120, 200))
    
    # Top header separator line
    draw.line([25, 38, w - 20, 38], fill=(120, 95, 55, 180), width=1)
    
    img.save('Assets/Image/UI/Quest_Banner.png')
    print("Quest_Banner.png created")

def create_quest_badge():
    size = 64
    img = Image.new('RGBA', (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)
    
    # Wax seal / gold crest
    draw.ellipse([4, 4, size - 5, size - 5], fill=(200, 155, 50, 255))
    draw.ellipse([8, 8, size - 9, size - 9], fill=(240, 195, 80, 255))
    draw.ellipse([12, 12, size - 13, size - 13], fill=(180, 135, 35, 255))
    
    # Scroll / exclamation symbol inside
    draw.polygon([(26, 18), (38, 18), (35, 38), (29, 38)], fill=(255, 245, 210, 255))
    draw.ellipse([29, 42, 35, 48], fill=(255, 245, 210, 255))
    
    img.save('Assets/Image/UI/Quest_Badge.png')
    print("Quest_Badge.png created")

create_dash_ring()
create_dash_mask()
create_dash_icon()
create_quest_banner()
create_quest_badge()
