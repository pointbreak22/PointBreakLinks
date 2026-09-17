import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FadeInOnScroll } from '../../shared/directives/fade-in-on-scroll';
import { Footer } from '../../shared/layout/footer/footer';
import { Header } from '../../shared/layout/header/header';

interface StatItem {
  value: string;
  label: string;
}

interface FeatureItem {
  icon: string;
  title: string;
  description: string;
}

interface PricingPlan {
  title: string;
  price: string;
  features: string[];
  featured?: boolean;
}

interface RoleCard {
  icon: string;
  title: string;
  description: string;
  features: string[];
  cta: string;
}

interface ProcessStep {
  title: string;
  description: string;
}

interface Faq {
  question: string;
  answer: string;
}

interface Testimonial {
  initials: string;
  text: string;
  name: string;
  role: string;
}

interface TrafficPoint {
  month: string;
  value: number;
  heightPercent: number;
}

// Ported from FOXLinks' pages/index.vue — the public marketing landing page.
@Component({
  selector: 'app-landing',
  imports: [RouterLink, DecimalPipe, FadeInOnScroll, Header, Footer],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './landing.html',
})
export class Landing {
  protected readonly heroStats: StatItem[] = [
    { value: '8,421', label: 'пользователей' },
    { value: '156,847', label: 'размещенных ссылок' },
    { value: '98%', label: 'успешных сделок' },
  ];

  protected readonly trafficChart: TrafficPoint[] = [
    { month: 'Янв', value: 6500, heightPercent: 60 },
    { month: 'Фев', value: 7100, heightPercent: 65 },
    { month: 'Мар', value: 7600, heightPercent: 70 },
    { month: 'Апр', value: 8200, heightPercent: 75 },
    { month: 'Май', value: 9300, heightPercent: 85 },
    { month: 'Июн', value: 10900, heightPercent: 100 },
  ];

  protected readonly quickActions: FeatureItem[] = [
    { icon: 'fa-infinity', title: 'Вечные ссылки', description: 'Пожизненное размещение на качественных площадках' },
    { icon: 'fa-home', title: 'Ссылки с главных', description: 'Быстрый результат с максимальным весом' },
    { icon: 'fa-sitemap', title: 'Многоуровневые', description: 'Усиление эффекта от полученных беклинков' },
    { icon: 'fa-users', title: 'Крауд-ссылки', description: 'Естественное ссылочное окружение' },
  ];

  protected readonly numbersStats: (StatItem & { trend: string })[] = [
    { value: '98%', label: 'Успешных размещений', trend: '+2% за год' },
    { value: '48ч', label: 'Среднее время выполнения', trend: 'на 30% быстрее' },
    { value: '24/7', label: 'Поддержка клиентов', trend: 'ответ за 15 мин' },
    { value: '1,046', label: 'Запросов в ТОП', trend: '+156 за месяц' },
  ];

  protected readonly features: FeatureItem[] = [
    {
      icon: 'fa-infinity',
      title: 'Вечные ссылки',
      description:
        'Естественное SEO-продвижение с помощью размещения статей и вечных ссылок на уникальных площадках с пожизненным размещением.',
    },
    {
      icon: 'fa-home',
      title: 'Ссылки с главных',
      description: 'Временные ссылки со страниц с наибольшим весом. Быстрый результат, гибкая настройка под требования кампании.',
    },
    {
      icon: 'fa-sitemap',
      title: 'Многоуровневые ссылки',
      description: 'Усиление эффекта от уже полученных беклинков при помощи ссылок 2-го и 3-го уровня для максимального эффекта.',
    },
    {
      icon: 'fa-users',
      title: 'Крауд-ссылки',
      description: 'Бэклинки с уникальным околоссылочным окружением с разных тематических форумов, блогов и социальных платформ.',
    },
    {
      icon: 'fa-chart-line',
      title: 'Усиление ссылок',
      description: 'Повышение поведенческих факторов ранее размещенной вечной ссылки через естественные пользовательские переходы.',
    },
    {
      icon: 'fa-chart-bar',
      title: 'Аналитическая платформа',
      description: 'Все необходимые инструменты для ежедневной работы SEO специалиста: аудит, мониторинг позиций, анализ конкурентов.',
    },
  ];

  protected readonly pricingPlans: PricingPlan[] = [
    {
      title: 'Стартовый',
      price: '990 ₽',
      features: ['До 10 ссылок в месяц', 'Базовые метрики', 'Email поддержка'],
    },
    {
      title: 'Профессиональный',
      price: '2 990 ₽',
      features: ['До 50 ссылок в месяц', 'Расширенные метрики', 'Приоритетная поддержка', 'AI-подбор площадок'],
      featured: true,
    },
    {
      title: 'Корпоративный',
      price: '7 990 ₽',
      features: ['Неограниченное количество ссылок', 'Все метрики и отчеты', 'Персональный менеджер', 'API доступ'],
    },
  ];

  protected readonly roleCards: RoleCard[] = [
    {
      icon: 'fa-shopping-cart',
      title: 'Для оптимизаторов',
      description:
        'Рост поискового трафика за счет ссылочного продвижения. Доступ к тысячам качественных площадок с различными тематиками и показателями.',
      features: [
        'Доступ к 8,401+ площадкам',
        'Гарантия индексации ссылок',
        'Подробная аналитика эффективности',
        'Подбор релевантных площадок',
      ],
      cta: 'Начать покупку ссылок',
    },
    {
      icon: 'fa-store',
      title: 'Для вебмастеров',
      description: 'Пассивный доход сайта за счет ручного размещения ссылок. Монетизируйте ваш трафик и получайте стабильный доход.',
      features: [
        'Высокие ставки за размещение',
        'Быстрые выплаты без задержек',
        'Гарантия оплаты выполненных работ',
        'Простая система управления заказами',
      ],
      cta: 'Начать продавать ссылки',
    },
  ];

  protected readonly processSteps: ProcessStep[] = [
    { title: 'Регистрация', description: 'Быстрая регистрация с подтверждением профиля. Широкие возможности пополнения аккаунта различными способами.' },
    { title: 'Добавление проекта', description: 'Укажите продвигаемый URL и необходимые параметры. Подберите релевантные площадки с помощью умного фильтра.' },
    { title: 'Выполнение задач', description: 'Отслеживайте статус выполнения задач в реальном времени. Гарантируем качественное выполнение услуг веб-мастерами.' },
    { title: 'Анализ результатов', description: 'Анализируйте эффект от ссылочного продвижения с помощью наших инструментов аналитики и отчетов.' },
  ];

  protected readonly faqs: Faq[] = [
    {
      question: 'Как быстро индексируются ссылки?',
      answer: 'Большинство ссылок индексируется в течение 2-4 недель. Мы предоставляем гарантию индексации и отслеживаем статус каждой ссылки.',
    },
    {
      question: 'Как происходит выплата вебмастерам?',
      answer: 'Выплаты производятся автоматически каждый понедельник на указанные реквизиты. Минимальная сумма для вывода — 500 рублей.',
    },
    {
      question: 'Можно ли отменить заказ?',
      answer: 'Да, вы можете отменить заказ в течение 24 часов после размещения, если он еще не был взят в работу вебмастером.',
    },
    {
      question: 'Есть ли ограничения по тематикам сайтов?',
      answer: 'Мы работаем с большинством легальных тематик. Запрещены только сайты с запрещенным контентом (наркотики, оружие, порнография и т.д.).',
    },
  ];

  protected readonly testimonials: Testimonial[] = [
    {
      initials: 'АК',
      text: 'За 3 месяца работы с PointbreakLinks мой сайт поднялся в ТОП-10 по 12 ключевым запросам. Качество ссылок отличное, поддержка всегда на связи!',
      name: 'Алексей К.',
      role: 'SEO-специалист',
    },
    {
      initials: 'МС',
      text: 'Размещаю ссылки на своих 5 сайтах уже больше года. Стабильный доход, быстрые выплаты. Лучшая биржа для вебмастеров!',
      name: 'Марина С.',
      role: 'Владелец сайтов',
    },
    {
      initials: 'ДИ',
      text: 'Перепробовал много бирж, остановился на PointbreakLinks. Удобный интерфейс, адекватные цены, много качественных площадок. Рекомендую!',
      name: 'Дмитрий И.',
      role: 'Интернет-маркетолог',
    },
  ];

  protected readonly activeFaqIndex = signal(-1);

  toggleFaq(index: number): void {
    this.activeFaqIndex.update((current) => (current === index ? -1 : index));
  }
}
