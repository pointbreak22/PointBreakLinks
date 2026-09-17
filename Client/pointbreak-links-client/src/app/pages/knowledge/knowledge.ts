import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Header } from '../../shared/layout/header/header';

interface FaqItem {
  question: string;
  answer: string;
}

interface FaqCategory {
  title: string;
  icon: string;
  items: FaqItem[];
}

// Backs the "База знаний" link in header.html/footer.html (plain <a href> pointing at a route
// that never existed — FOXLinks' own knowledge.vue was a literally empty stub, see
// pages/_NEXT.md) and the sidebar's "База знаний" link (previously coming-soon). Static content,
// not a CMS — the platform doesn't need editable articles, it needs accurate ones, and every
// answer below describes how this app's real, already-built features actually behave (no
// aspirational "coming soon" claims).
const CATEGORIES: FaqCategory[] = [
  {
    title: 'Начало работы',
    icon: 'fa-rocket',
    items: [
      {
        question: 'Как зарегистрироваться?',
        answer:
          'Нажмите «Регистрация», укажите логин, email и пароль (минимум 8 символов, заглавная буква и цифра). Аккаунт универсальный — вы можете одновременно и покупать ссылки под свои проекты, и продавать размещения на своих площадках.',
      },
      {
        question: 'Я забыл пароль, что делать?',
        answer:
          'На странице входа нажмите «Забыли пароль?», укажите email — на него придёт письмо со ссылкой для сброса (ссылка действует 1 час).',
      },
    ],
  },
  {
    title: 'Покупка ссылок',
    icon: 'fa-shopping-cart',
    items: [
      {
        question: 'Как разместить ссылку на чужой площадке?',
        answer:
          'Создайте проект в разделе «Мои проекты», затем перейдите в «Покупка ссылок», подберите площадку по фильтрам (тематика, страна, цена, ИКС, DR) и нажмите «Купить». Стоимость сразу списывается с баланса.',
      },
      {
        question: 'Что означают статусы заказа?',
        answer:
          '«Заявка» — заказ создан и ждёт, пока продавец возьмёт его в работу. «В работе» — продавец принял заказ. «Опубликовано» — продавец подтвердил размещение ссылки. Каждый шаг фиксируется в истории заказа.',
      },
      {
        question: 'Могу я написать продавцу площадки?',
        answer: 'Да — у каждого заказа есть чат с продавцом (кнопка «Чат» в списке заказов), а все переписки собраны в разделе «Сообщения».',
      },
    ],
  },
  {
    title: 'Продажа ссылок',
    icon: 'fa-store',
    items: [
      {
        question: 'Как добавить свою площадку?',
        answer:
          'В разделе «Продажа ссылок» → «Мои площадки» нажмите «Добавить площадку» и укажите URL, тематику, цену и параметры сайта. Площадка отправляется на модерацию.',
      },
      {
        question: 'Как подтвердить владение площадкой?',
        answer:
          'Разместите на сайте выданный meta-тег или текстовый файл с кодом подтверждения, затем нажмите «Проверить» рядом с площадкой — система сама зайдёт на сайт и проверит код.',
      },
      {
        question: 'Почему площадка «На модерации»?',
        answer: 'Каждая новая площадка проверяется модератором перед тем, как попасть в общий каталог — это занимает некоторое время.',
      },
    ],
  },
  {
    title: 'Кошелёк и оплата',
    icon: 'fa-wallet',
    items: [
      {
        question: 'Как пополнить баланс?',
        answer:
          'В разделе «Кошелёк» укажите сумму и нажмите «Пополнить». Сейчас пополнение зачисляется мгновенно — интеграция с реальным платёжным провайдером ещё не подключена.',
      },
      {
        question: 'Где посмотреть историю операций?',
        answer: 'В разделе «Кошелёк» под формой пополнения — там видны все списания (покупки) и начисления (продажи, пополнения).',
      },
      {
        question: 'Когда деньги поступают продавцу?',
        answer: 'В момент, когда продавец принимает заказ в работу — до этого момента средства уже списаны с покупателя, но ещё не зачислены продавцу.',
      },
    ],
  },
  {
    title: 'Отзывы',
    icon: 'fa-star',
    items: [
      {
        question: 'Когда можно оставить отзыв о площадке?',
        answer: 'После того как продавец подтвердит публикацию по вашему заказу. Один отзыв — на один заказ.',
      },
      {
        question: 'Где посмотреть отзывы о площадке?',
        answer: 'Нажмите на рейтинг (★ и число отзывов) рядом с площадкой в каталоге или в «Моих площадках» — откроется список отзывов.',
      },
    ],
  },
];

@Component({
  selector: 'app-knowledge',
  imports: [Header, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './knowledge.html',
})
export class Knowledge {
  protected readonly categories = CATEGORIES;
  protected readonly openKey = signal<string | null>(null);

  toggle(categoryIndex: number, itemIndex: number): void {
    const key = `${categoryIndex}-${itemIndex}`;
    this.openKey.update((current) => (current === key ? null : key));
  }

  isOpen(categoryIndex: number, itemIndex: number): boolean {
    return this.openKey() === `${categoryIndex}-${itemIndex}`;
  }
}
