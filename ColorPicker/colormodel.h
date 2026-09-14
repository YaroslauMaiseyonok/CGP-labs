#ifndef COLORMODEL_H
#define COLORMODEL_H

#include <QObject>
#include <QString>
#include <cmath>
#include <algorithm>

struct Vec3
{
    double x = 0.0, y = 0.0, z = 0.0;
    Vec3() = default;
    Vec3(double x_, double y_, double z_) : x(x_), y(y_), z(z_) {}

    double  operator[](int i) const { return i == 0 ? x : (i == 1 ? y : z); }
    double& operator[](int i)       { return i == 0 ? x : (i == 1 ? y : z); }
};

struct Mat3
{
    double m[3][3] = {{0,0,0},{0,0,0},{0,0,0}};
    Vec3 mul(const Vec3& v) const
    {
        return Vec3(m[0][0]*v.x + m[0][1]*v.y + m[0][2]*v.z,
                    m[1][0]*v.x + m[1][1]*v.y + m[1][2]*v.z,
                    m[2][0]*v.x + m[2][1]*v.y + m[2][2]*v.z);
    }
};

inline double clamp01(double v) { return std::min(1.0, std::max(0.0, v)); }
inline Vec3 clampVec(const Vec3& v) { return Vec3(clamp01(v.x), clamp01(v.y), clamp01(v.z)); }
inline double compMin(const Vec3& v) { return std::min(v.x, std::min(v.y, v.z)); }
inline double compMax(const Vec3& v) { return std::max(v.x, std::max(v.y, v.z)); }

class ColorModel : public QObject
{
    Q_OBJECT
public:
    enum class Illuminant    { D65, D50, E };
    Q_ENUM(Illuminant)
    enum class GamutStrategy { Clipping, Scaling };
    Q_ENUM(GamutStrategy)

    struct GamutFit
    {
        Vec3 linear;
        Vec3 encoded;
        bool outOfGamut = false;
        bool modified   = false;
    };

    explicit ColorModel(QObject* parent = nullptr);

    void setIlluminant(Illuminant il);
    Illuminant illuminant() const { return currentIlluminant; }

    void setGamutStrategy(GamutStrategy s);
    GamutStrategy gamutStrategy() const { return strategy; }

    void setColorFromRgb(const Vec3& encoded);
    void setColorFromRgb8(int r, int g, int b);
    void setColorFromXyz(const Vec3& xyz);
    void setColorFromHsl(const Vec3& hsl);

    Vec3 xyz()  const { return currentXyz; }
    Vec3 rgb()  const { return lastFit.encoded; }
    Vec3 rgb8() const;
    Vec3 hsl()  const;
    QString gamutWarning() const;

    GamutFit xyzToRgb(const Vec3& xyz) const;

    static Vec3 hslToRgb(const Vec3& hsl);

    double xyzChannelMax() const;
    QString matrixDebugString() const;

signals:
    void colorChanged();

private:
    void rebuildMatrices();
    void refreshFit();
    GamutFit fitToGamut(const Vec3& linear) const;
    Vec3 rgbToXyz(const Vec3& encoded) const;

    static double srgbDecode(double c);
    static double srgbEncode(double c);
    static Vec3 decode(const Vec3& v);
    static Vec3 encode(const Vec3& v);
    static Vec3 rgbToHsl(const Vec3& encoded);
    static Mat3 invert3x3(const Mat3& a);
    static Vec3 whitePointChromaticity(Illuminant il);
    Vec3 whitePointXyz() const;
    Vec3 hslHint {0.0, 0.0, 0.5};
    bool hslHintValid = false;

    Illuminant    currentIlluminant = Illuminant::D65;
    GamutStrategy strategy = GamutStrategy::Clipping;
    Mat3 M;
    Mat3 Minv;
    Vec3 currentXyz {0.0, 0.0, 0.0};
    GamutFit lastFit;
};

#endif