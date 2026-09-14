#ifndef GRADIENTSLIDER_H
#define GRADIENTSLIDER_H

#include <QAbstractSlider>
#include <QGradientStops>

class GradientSlider : public QAbstractSlider
{
    Q_OBJECT
public:
    explicit GradientSlider(QWidget* parent = nullptr);

    void setStops(const QGradientStops& stops);

    QSize sizeHint() const override        { return QSize(200, 28); }
    QSize minimumSizeHint() const override { return QSize(80, 24); }

protected:
    void paintEvent(QPaintEvent* e) override;
    void mousePressEvent(QMouseEvent* e) override;
    void mouseMoveEvent(QMouseEvent* e) override;
    void mouseReleaseEvent(QMouseEvent* e) override;

private:
    QRect grooveRect() const;
    int   valueAt(const QPoint& pos) const;

    QGradientStops stops;
    bool dragging = false;
};

#endif