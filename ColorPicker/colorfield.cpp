#include "colorfield.h"

#include <QPainter>
#include <QMouseEvent>
#include <QCursor>
#include <algorithm>

namespace {
constexpr int kMaxRes = 256;
}

ColorField::ColorField(QWidget* parent)
    : QWidget(parent)
{
    setSizePolicy(QSizePolicy::Expanding, QSizePolicy::Expanding);
    updateHueCoeffs();
}

double ColorField::hueCoeff(double t)
{
    if (t < 1.0/6.0) return 6.0 * t;
    if (t < 1.0/2.0) return 1.0;
    if (t < 2.0/3.0) return 6.0 * (2.0/3.0 - t);
    return 0.0;
}

void ColorField::updateHueCoeffs()
{
    auto wrap = [](double v) { return v - std::floor(v); };
    k[0] = hueCoeff(wrap(hue + 1.0/3.0));
    k[1] = hueCoeff(wrap(hue));
    k[2] = hueCoeff(wrap(hue - 1.0/3.0));
}

void ColorField::setHue(double h)
{
    h -= std::floor(h);
    if (std::abs(h - hue) < 1e-4) return;
    hue = h;
    updateHueCoeffs();
    rebuildImage();
    update();
}

void ColorField::setMarker(double sat, double lit)
{
    s = std::clamp(sat, 0.0, 1.0);
    l = std::clamp(lit, 0.0, 1.0);
    update();
}

void ColorField::rebuildImage()
{
    const int w = std::clamp(width(),  2, kMaxRes);
    const int h = std::clamp(height(), 2, kMaxRes);
    if (img.width() != w || img.height() != h)
        img = QImage(w, h, QImage::Format_RGB32);
    if (img.isNull()) return;

    for (int y = 0; y < h; ++y) {
        const double light = 1.0 - y / double(h - 1);
        const double a = std::min(light, 1.0 - light);
        const double mr = a * (2.0 * k[0] - 1.0);
        const double mg = a * (2.0 * k[1] - 1.0);
        const double mb = a * (2.0 * k[2] - 1.0);
        QRgb* line = reinterpret_cast<QRgb*>(img.scanLine(y));
        for (int x = 0; x < w; ++x) {
            const double sat = x / double(w - 1);
            line[x] = qRgb(int((light + sat * mr) * 255.0 + 0.5),
                           int((light + sat * mg) * 255.0 + 0.5),
                           int((light + sat * mb) * 255.0 + 0.5));
        }
    }
}

QPointF ColorField::markerPos() const
{
    return QPointF(s * (width() - 1), (1.0 - l) * (height() - 1));
}

void ColorField::paintEvent(QPaintEvent*)
{
    QPainter p(this);
    if (img.isNull())
        rebuildImage();

    p.setRenderHint(QPainter::SmoothPixmapTransform);
    p.drawImage(rect(), img);

    p.setPen(QPen(QColor(0, 0, 0, 120), 1.0));
    p.setBrush(Qt::NoBrush);
    p.drawRect(rect().adjusted(0, 0, -1, -1));

    const QPointF c = markerPos();
    p.setBrush(Qt::NoBrush);
    p.setPen(QPen(Qt::black, 2.5));
    p.drawEllipse(c, 6.5, 6.5);
    p.setPen(QPen(Qt::white, 1.5));
    p.drawEllipse(c, 4.5, 4.5);
}

void ColorField::pick(const QPoint& pos)
{
    const double sat = std::clamp(pos.x() / double(qMax(1, width() - 1)), 0.0, 1.0);
    const double lit = std::clamp(1.0 - pos.y() / double(qMax(1, height() - 1)), 0.0, 1.0);
    s = sat;
    l = lit;
    update();
    emit picked(sat, lit);
}

void ColorField::mousePressEvent(QMouseEvent* e)
{
    if (e->button() == Qt::LeftButton) {
        dragging = true;
        pick(e->pos());
        e->accept();
    }
}

void ColorField::mouseMoveEvent(QMouseEvent* e)
{
    if (dragging) {
        pick(e->pos());
        e->accept();
    }
}

void ColorField::mouseReleaseEvent(QMouseEvent* e)
{
    dragging = false;
    QWidget::mouseReleaseEvent(e);
}

void ColorField::resizeEvent(QResizeEvent* e)
{
    QWidget::resizeEvent(e);
    rebuildImage();
}