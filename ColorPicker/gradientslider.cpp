#include "gradientslider.h"

#include <QPainter>
#include <QMouseEvent>
#include <QCursor>

GradientSlider::GradientSlider(QWidget* parent)
    : QAbstractSlider(parent)
{
    setRange(0, 4095);
    setSingleStep(41);
    setPageStep(410);
    setFocusPolicy(Qt::StrongFocus);
    setSizePolicy(QSizePolicy::Expanding, QSizePolicy::Fixed);
}

void GradientSlider::setStops(const QGradientStops& stops)
{
    this->stops = stops;
    update();
}

QRect GradientSlider::grooveRect() const
{
    const int halfHandle = 8;
    const int h = 12;
    return QRect(halfHandle, height() / 2 - h / 2,
                 qMax(1, width() - 2 * halfHandle), h);
}

int GradientSlider::valueAt(const QPoint& pos) const
{
    const QRect g = grooveRect();
    double t = (pos.x() - g.left()) / double(g.width());
    t = qBound(0.0, t, 1.0);
    return minimum() + int(t * (maximum() - minimum()) + 0.5);
}

void GradientSlider::paintEvent(QPaintEvent*)
{
    QPainter p(this);
    p.setRenderHint(QPainter::Antialiasing);

    const QRect g = grooveRect();

    QLinearGradient grad(g.topLeft(), g.topRight());
    if (stops.isEmpty()) {
        grad.setColorAt(0.0, palette().color(QPalette::Dark));
        grad.setColorAt(1.0, palette().color(QPalette::Light));
    } else {
        grad.setStops(stops);
    }
    p.setPen(QPen(QColor(0, 0, 0, 90), 1.0));
    p.setBrush(grad);
    p.drawRoundedRect(g, 5, 5);

    const double span = maximum() - minimum();
    const double t = span <= 0 ? 0.0 : (value() - minimum()) / span;
    const int hx = g.left() + int(t * g.width());
    const QRectF handle(hx - 6, g.top() - 5, 12, g.height() + 10);

    p.setBrush(Qt::NoBrush);
    p.setPen(QPen(QColor(0, 0, 0, 180), 2.5));
    p.drawRoundedRect(handle, 4, 4);
    p.setPen(QPen(QColor(255, 255, 255, 230), 1.2));
    p.drawRoundedRect(handle.adjusted(1.6, 1.6, -1.6, -1.6), 3, 3);

    if (hasFocus()) {
        p.setPen(QPen(palette().color(QPalette::Highlight), 1.0));
        p.setBrush(Qt::NoBrush);
        p.drawRoundedRect(rect().adjusted(1, 1, -1, -1), 5, 5);
    }
}

void GradientSlider::mousePressEvent(QMouseEvent* e)
{
    if (e->button() == Qt::LeftButton) {
        dragging = true;
        setValue(valueAt(e->pos()));
        e->accept();
    } else {
        QAbstractSlider::mousePressEvent(e);
    }
}

void GradientSlider::mouseMoveEvent(QMouseEvent* e)
{
    if (dragging) {
        setValue(valueAt(e->pos()));
        e->accept();
    } else {
        QAbstractSlider::mouseMoveEvent(e);
    }
}

void GradientSlider::mouseReleaseEvent(QMouseEvent* e)
{
    dragging = false;
    QAbstractSlider::mouseReleaseEvent(e);
}