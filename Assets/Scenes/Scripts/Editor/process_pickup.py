import os
from PIL import Image

def process_file_smart(img_path, out_prefix, scale_factor=1.3387):
    out_dir = r"C:\Users\Artemis\rootbound-guardian\Assets\Character\FinalMC"
    
    if not os.path.exists(img_path):
        print(f"Error: Source image not found at {img_path}")
        return
        
    img = Image.open(img_path).convert("RGBA")
    width, height = img.size
    print(f"Processing {out_prefix}: {width}x{height}")
    
    # 1. Flood fill background to make it transparent
    pixels = img.load()
    visited = set()
    queue = []
    
    # Add border pixels to queue
    for x in range(width):
        queue.append((x, 0))
        queue.append((x, height - 1))
    for y in range(1, height - 1):
        queue.append((0, y))
        queue.append((width - 1, y))
        
    tolerance = 40
    def is_white_ish(color):
        r, g, b, a = color
        return (255 - r)**2 + (255 - g)**2 + (255 - b)**2 < tolerance**2
        
    bg_pixels = set()
    for start in queue:
        if start in visited:
            continue
        r, g, b, a = pixels[start[0], start[1]]
        if is_white_ish((r, g, b, a)):
            q = [start]
            visited.add(start)
            while q:
                cx, cy = q.pop(0)
                bg_pixels.add((cx, cy))
                for dx, dy in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
                    nx, ny = cx + dx, cy + dy
                    if 0 <= nx < width and 0 <= ny < height:
                        if (nx, ny) not in visited:
                            color = pixels[nx, ny]
                            if is_white_ish(color):
                                visited.add((nx, ny))
                                q.append((nx, ny))
                                
    # Make background transparent
    for x, y in bg_pixels:
        pixels[x, y] = (0, 0, 0, 0)
        
    # 2. Find optimal cutting points using vertical pixel density
    density = []
    for x in range(width):
        col_density = sum(1 for y in range(height) if pixels[x, y][3] > 0)
        density.append(col_density)
        
    # Dynamic search ranges based on fractions of image width
    w_quarter = width // 4
    w_half = width // 2
    w_three_quarters = (3 * width) // 4
    
    # Search for first cut (between Frame 1 and 2)
    cut1 = w_half
    min_d1 = 999999
    for x in range(w_quarter, w_half):
        if density[x] <= min_d1:
            min_d1 = density[x]
            cut1 = x
            
    # Search for second cut (between Frame 2 and 3)
    cut2 = w_three_quarters
    min_d2 = 999999
    for x in range(w_half, w_three_quarters):
        if density[x] <= min_d2:
            min_d2 = density[x]
            cut2 = x
            
    print(f"Dynamic cut points: cut1={cut1} (density={min_d1}), cut2={cut2} (density={min_d2})")
    
    # 3. Slice and process columns
    slices = [
        (0, cut1),
        (cut1, cut2),
        (cut2, width)
    ]
    
    for i, (left, right) in enumerate(slices):
        col_img = img.crop((left, 0, right, height))
        
        # Erase small bleeding edge pixels at the cut boundaries to prevent artifacts
        col_pixels = col_img.load()
        col_w, col_h = col_img.size
        # Clear 3 pixels on left and right borders of the slice
        for x in range(min(3, col_w)):
            for y in range(col_h):
                col_pixels[x, y] = (0, 0, 0, 0)
                col_pixels[col_w - 1 - x, y] = (0, 0, 0, 0)
                
        # Get bounding box of the sliced column contents
        bbox = col_img.getbbox()
        if not bbox:
            print(f"Skipping empty column {i}")
            continue
            
        # Crop to contents
        cropped_comp = col_img.crop(bbox)
        crop_w, crop_h = cropped_comp.size
        
        # Scale cropped image
        scaled_w = int(crop_w * scale_factor)
        scaled_h = int(crop_h * scale_factor)
        scaled_comp = cropped_comp.resize((scaled_w, scaled_h), Image.Resampling.LANCZOS)
        
        # Keep feet at Y=919
        p_y = 919 - scaled_h
        
        # Center horizontally in the 1024x1024 canvas
        p_x = 512 - (scaled_w // 2)
        
        # Create final 1024x1024 canvas
        canvas = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
        canvas.paste(scaled_comp, (p_x, p_y), scaled_comp)
        
        # Save as PNG
        out_name = f"{out_prefix}{i+1:02d}.png"
        out_path = os.path.join(out_dir, out_name)
        canvas.save(out_path, "PNG")
        print(f"Saved: {out_path} (Size: 1024x1024)")

def main():
    img_path = r"C:\Users\Artemis\.gemini\antigravity-cli\brain\9a02e2fb-52fe-44e0-a0c4-7fb735d6067b\elf_girl_pickup_1787627435495.jpg"
    process_file_smart(img_path, "Pickup", 1.3387)

if __name__ == "__main__":
    main()
