export default function (key: string, fallback?: string) {
    const fullKey = `K45::EUIS.main[${key}]`;
    const tr = engine.translate(`K45::EUIS.main[${key}]`);
    if (tr === fullKey) {
        if (fallback !== undefined) {
            return fallback;
        }
        (window as any).K45_MISSING_I18N ??= new Set<string>();
        ((window as any).K45_MISSING_I18N as Set<string>).add(fullKey);
    }
    return tr;
}

