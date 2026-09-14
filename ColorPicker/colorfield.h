#ifndef COLORFIELD_H
#define COLORFIELD_H

#include <QWidget>
#include <QImage>

class ColorField : public QWidget
{
    Q_OBJECT
public:
    explicit ColorField(QWidget* parent = nullptr);

    void setHue(double h);
    void setMarker(double sat, double lit);

    QSize sizeHint() const override        { return QSize(240, 200); }
    QSize minimumSizeHint() const override { return QSize(140, 120); }

signals:
    void picked(double s, double l);

protected:
    void paintEvent(QPaintEvent* e) override;
    void mousePressEvent(QMouseEvent* e) override;
    void mouseMoveEvent(QMouseEvent* e) override;
    void mouseReleaseEvent(QMouseEvent* e) override;
    void resizeEvent(QResizeEvent* e) override;

private:
    void rebuildImage();
    void updateHueCoeffs();
    QPointF markerPos() const;
    void pick(const QPoint& pos);
    static double hueCoeff(double t);

    QImage img;
    double hue = 0.0;
    double k[3] = {1.0, 0.0, 0.0};
    double s = 0.0, l = 0.5;
    bool dragging = false;
};

#endif