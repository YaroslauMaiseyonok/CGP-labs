#include "colormodel.h"

namespace {
constexpr double EPS = 1e-9;
constexpr double XR = 0.6400, YR = 0.3300;
constexpr double XG = 0.3000, YG = 0.6000;
constexpr double XB = 0.1500, YB = 0.0600;
}

ColorModel::ColorModel(QObject* parent)
    : QObject(parent)
{
    rebuildMatrices();
    currentXyz = M.mul(decode(Vec3(0.5, 0.5, 0.5)));
    refreshFit();
}

void ColorModel::setIlluminant(Illuminant il)
{
    if (currentIlluminant == il) return;
    currentIlluminant = il;
    rebuildMatrices();
    refreshFit();
    emit colorChanged();
}

void ColorModel::setGamutStrategy(GamutStrategy s)
{
    if (strategy == s) return;
    strategy = s;
    refreshFit();
    emit colorChanged();
}

Vec3 ColorModel::whitePointChromaticity(Illuminant il)
{
    switch (il) {
    case Illuminant::D50: return Vec3(0.34567, 0.35850, 0.0);
    case Illuminant::E:   return Vec3(1.0 / 3.0, 1.0 / 3.0, 0.0);
    case Illuminant::D65:
    default:              return Vec3(0.31272, 0.32903, 0.0);
    }
}

void ColorModel::rebuildMatrices()
{
    const Vec3 w = whitePointChromaticity(currentIlluminant);

    Mat3 P;
    P.m[0][0] = XR / YR;  P.m[0][1] = XG / YG;  P.m[0][2] = XB / YB;
    P.m[1][0] = 1.0;      P.m[1][1] = 1.0;      P.m[1][2] = 1.0;
    P.m[2][0] = (1.0 - XR - YR) / YR;
    P.m[2][1] = (1.0 - XG - YG) / YG;
    P.m[2][2] = (1.0 - XB - YB) / YB;

    const Vec3 W(w.x / w.y, 1.0, (1.0 - w.x - w.y) / w.y);
    const Vec3 S = invert3x3(P).mul(W);
    const double sv[3] = { S.x, S.y, S.z };

    for (int r = 0; r < 3; ++r)
        for (int c = 0; c < 3; ++c)
            M.m[r][c] = P.m[r][c] * sv[c];

    Minv = invert3x3(M);
}

Mat3 ColorModel::invert3x3(const Mat3& a)
{
    const double A = a.m[0][0], B = a.m[0][1], C = a.m[0][2];
    const double D = a.m[1][0], E = a.m[1][1], F = a.m[1][2];
    const double G = a.m[2][0], H = a.m[2][1], I = a.m[2][2];
    const double det = A*(E*I - F*H) - B*(D*I - F*G) + C*(D*H - E*G);

    Mat3 r;
    r.m[0][0] =  (E*I - F*H) / det;
    r.m[0][1] = -(B*I - C*H) / det;
    r.m[0][2] =  (B*F - C*E) / det;
    r.m[1][0] = -(D*I - F*G) / det;
    r.m[1][1] =  (A*I - C*G) / det;
    r.m[1][2] = -(A*F - C*D) / det;
    r.m[2][0] =  (D*H - E*G) / det;
    r.m[2][1] = -(A*H - B*G) / det;
    r.m[2][2] =  (A*E - B*D) / det;
    return r;
}

double ColorModel::srgbDecode(double c)
{
    c = clamp01(c);
    return c <= 0.04045 ? c / 12.92 : std::pow((c + 0.055) / 1.055, 2.4);
}

double ColorModel::srgbEncode(double c)
{
    c = clamp01(c);
    return c <= 0.0031308 ? 12.92 * c : 1.055 * std::pow(c, 1.0 / 2.4) - 0.055;
}

Vec3 ColorModel::decode(const Vec3& v)
{ return Vec3(srgbDecode(v.x), srgbDecode(v.y), srgbDecode(v.z)); }

Vec3 ColorModel::encode(const Vec3& v)
{ return Vec3(srgbEncode(v.x), srgbEncode(v.y), srgbEncode(v.z)); }

Vec3 ColorModel::rgbToHsl(const Vec3& c)
{
    const double mx = compMax(c), mn = compMin(c);
    const double l = (mx + mn) * 0.5;
    if (mx - mn < EPS) return Vec3(0.0, 0.0, l);

    const double d = mx - mn;
    const double s = (l > 0.5) ? d / (2.0 - mx - mn) : d / (mx + mn);

    double h;
    if      (mx == c.x) h = (c.y - c.z) / d + (c.y < c.z ? 6.0 : 0.0);
    else if (mx == c.y) h = (c.z - c.x) / d + 2.0;
    else                h = (c.x - c.y) / d + 4.0;
    h /= 6.0;
    return Vec3(h, s, l);
}

Vec3 ColorModel::hslToRgb(const Vec3& hsl)
{
    double h = hsl.x - std::floor(hsl.x);
    const double s = clamp01(hsl.y);
    const double l = clamp01(hsl.z);
    if (s < EPS) return Vec3(l, l, l);

    auto hue = [](double p, double q, double t) {
        if (t < 0.0) t += 1.0;
        if (t > 1.0) t -= 1.0;
        if (t < 1.0/6.0) return p + (q - p) * 6.0 * t;
        if (t < 1.0/2.0) return q;
        if (t < 2.0/3.0) return p + (q - p) * (2.0/3.0 - t) * 6.0;
        return p;
    };

    const double q = (l < 0.5) ? l * (1.0 + s) : l + s - l * s;
    const double p = 2.0 * l - q;
    return Vec3(hue(p, q, h + 1.0/3.0), hue(p, q, h), hue(p, q, h - 1.0/3.0));
}

ColorModel::GamutFit ColorModel::fitToGamut(const Vec3& lin) const
{
    GamutFit fit;
    const bool out = lin.x < -EPS || lin.x > 1.0 + EPS ||
                     lin.y < -EPS || lin.y > 1.0 + EPS ||
                     lin.z < -EPS || lin.z > 1.0 + EPS;
    fit.outOfGamut = out;

    if (!out) {
        fit.linear = lin;
        fit.modified = false;
        fit.encoded = encode(lin);
        return fit;
    }

    fit.modified = true;
    if (strategy == GamutStrategy::Clipping) {
        fit.linear = clampVec(lin);
    } else {
        const double g = clamp01(lin.y);
        double t = 1.0;
        auto limit = [&t, g](double c) {
            if      (c > g + EPS) t = std::min(t, (1.0 - g) / (c - g));
            else if (c < g - EPS) t = std::min(t, g / (g - c));
        };
        limit(lin.x); limit(lin.y); limit(lin.z);
        t = clamp01(t);
        fit.linear = Vec3(g + t * (lin.x - g),
                          g + t * (lin.y - g),
                          g + t * (lin.z - g));
    }
    fit.encoded = encode(fit.linear);
    return fit;
}

Vec3 ColorModel::rgbToXyz(const Vec3& encoded) const
{
    return M.mul(decode(encoded));
}

ColorModel::GamutFit ColorModel::xyzToRgb(const Vec3& xyz) const
{
    return fitToGamut(Minv.mul(xyz));
}

void ColorModel::refreshFit()
{
    lastFit = xyzToRgb(currentXyz);
    const Vec3 h = rgbToHsl(lastFit.encoded);
    if (h.y > 1e-6) {
        double d = h.x - hslHint.x;
        d -= std::floor(d + 0.5);
        hslHint.x += d;
        hslHint.y = h.y;
        hslHint.z = h.z;
        hslHintValid = true;
    }
}

void ColorModel::setColorFromRgb(const Vec3& encoded)
{
    currentXyz = rgbToXyz(clampVec(encoded));
    refreshFit();
    emit colorChanged();
}

void ColorModel::setColorFromRgb8(int r, int g, int b)
{
    setColorFromRgb(Vec3(r / 255.0, g / 255.0, b / 255.0));
}

void ColorModel::setColorFromXyz(const Vec3& xyz)
{
    currentXyz = xyz;
    refreshFit();
    emit colorChanged();
}

void ColorModel::setColorFromHsl(const Vec3& hsl)
{
    hslHint = hsl;
    hslHintValid = true;
    setColorFromRgb(hslToRgb(hsl));
}

Vec3 ColorModel::rgb8() const
{
    const Vec3 e = lastFit.encoded;
    return Vec3(std::round(e.x * 255.0), std::round(e.y * 255.0), std::round(e.z * 255.0));
}

Vec3 ColorModel::hsl() const
{
    Vec3 h = rgbToHsl(lastFit.encoded);
    if (!hslHintValid) return h;
    h.x = hslHint.x;
    if (h.y <= 1e-6 && (h.z <= 1e-6 || h.z >= 1.0 - 1e-6))
        h.y = hslHint.y;
    return h;
}

QString ColorModel::gamutWarning() const
{
    if (!lastFit.modified) return QString();
    return (strategy == GamutStrategy::Clipping)
               ? QStringLiteral("Цвет вне охвата RGB, обрезан(Clipping)")
               : QStringLiteral("Цвет вне охвата RGB, сжат(Scaling)");
}

Vec3 ColorModel::whitePointXyz() const
{
    const Vec3 w = whitePointChromaticity(currentIlluminant);
    return Vec3(w.x / w.y, 1.0, (1.0 - w.x - w.y) / w.y);
}

double ColorModel::xyzChannelMax() const
{
    const Vec3 w = whitePointXyz();
    return std::max({1.0, w.x, w.z});
}

QString ColorModel::matrixDebugString() const
{
    const char* names[] = { "D65", "D50", "E" };
    QString s = QStringLiteral("Источник: %1\nRGB->XYZ:\n").arg(names[int(currentIlluminant)]);
    for (int r = 0; r < 3; ++r)
        s += QString("  %1 %2 %3\n")
                 .arg(M.m[r][0], 0, 'f', 7).arg(M.m[r][1], 0, 'f', 7).arg(M.m[r][2], 0, 'f', 7);
    s += QStringLiteral("XYZ->RGB:\n");
    for (int r = 0; r < 3; ++r)
        s += QString("  %1 %2 %3\n")
                 .arg(Minv.m[r][0], 0, 'f', 7).arg(Minv.m[r][1], 0, 'f', 7).arg(Minv.m[r][2], 0, 'f', 7);
    return s;
}