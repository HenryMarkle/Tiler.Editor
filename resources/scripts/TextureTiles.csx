Rectangle TriTile(Geo geo, float x, float y) {
    switch (geo) {
        case Geo.Solid: return new Rectangle(x * 20, y * 20, 20, 20);
        
        default: return new Rectangle(x * 20, y * 20, 20, 20);
    }
}